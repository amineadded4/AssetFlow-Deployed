// src/Backend/AssetFlow.Infrastructure/Services/ConversationHistoryService.cs
using AssetFlow.Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;
using AssetFlow.Application.DTOs;

namespace AssetFlow.Infrastructure.Services
{
    /// <summary>
    /// Stockage des conversations dans Redis avec TTL automatique de 30 jours.
    ///
    /// Schéma de clés :
    ///   conv:user:{userId}:index          → Sorted Set  (score = UpdatedAt epoch)
    ///                                         members = conversationId
    ///   conv:meta:{conversationId}        → Hash        (id, userId, title, createdAt, updatedAt, messageCount, lastMessage)
    ///   conv:msgs:{conversationId}        → List        (JSON de ConversationMessage, dernier en tête = index 0)
    /// </summary>
    public class ConversationHistoryService : IConversationHistoryService
    {
        private readonly IDatabase _redis;
        private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);

        public ConversationHistoryService(IConnectionMultiplexer mux)
        {
            _redis = mux.GetDatabase();
        }

        // ── Clés ─────────────────────────────────────────────────────────────
        private static string UserIndexKey(int userId)          => $"conv:user:{userId}:index";
        private static string MetaKey(string convId)            => $"conv:meta:{convId}";
        private static string MessagesKey(string convId)        => $"conv:msgs:{convId}";
        private static string AllConvsSetKey()                  => "conv:all"; // global set pour le purge

        // ── Créer une conversation ───────────────────────────────────────────
        public async Task<string> CreateConversationAsync(int userId, string title)
        {
            var convId = $"{userId}-{Guid.NewGuid():N}";
            var now    = DateTime.UtcNow;

            var meta = new HashEntry[]
            {
                new("id",           convId),
                new("userId",       userId),
                new("title",        title),
                new("createdAt",    now.ToString("O")),
                new("updatedAt",    now.ToString("O")),
                new("messageCount", 0),
                new("lastMessage",  "")
            };

            var batch = _redis.CreateBatch();

            // Stocker les métadonnées
            _ = batch.HashSetAsync(MetaKey(convId), meta);
            _ = batch.KeyExpireAsync(MetaKey(convId), Ttl);

            // Ajouter au sorted set de l'utilisateur (score = timestamp)
            _ = batch.SortedSetAddAsync(UserIndexKey(userId), convId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            _ = batch.KeyExpireAsync(UserIndexKey(userId), Ttl);

            // Ajouter au set global pour le purge
            _ = batch.SortedSetAddAsync(AllConvsSetKey(), $"{userId}:{convId}", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            batch.Execute();
            await Task.Delay(1); // force async

            return convId;
        }

        // ── Liste des conversations d'un utilisateur ─────────────────────────
        public async Task<List<ConversationSummary>> GetConversationsAsync(int userId)
        {
            // Récupérer les IDs triés par date décroissante
            var ids = await _redis.SortedSetRangeByRankAsync(
                UserIndexKey(userId), 0, -1, Order.Descending);

            var result = new List<ConversationSummary>();
            foreach (var id in ids)
            {
                var meta = await _redis.HashGetAllAsync(MetaKey(id!));
                if (meta.Length == 0) continue; // clé expirée

                var dict = meta.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
                result.Add(new ConversationSummary
                {
                    Id           = dict.GetValueOrDefault("id", id!),
                    UserId       = userId,
                    Title        = dict.GetValueOrDefault("title", "Conversation"),
                    CreatedAt    = DateTime.Parse(dict.GetValueOrDefault("createdAt", DateTime.UtcNow.ToString("O"))),
                    UpdatedAt    = DateTime.Parse(dict.GetValueOrDefault("updatedAt", DateTime.UtcNow.ToString("O"))),
                    MessageCount = int.TryParse(dict.GetValueOrDefault("messageCount", "0"), out var mc) ? mc : 0,
                    LastMessage  = dict.GetValueOrDefault("lastMessage", "")
                });
            }
            return result;
        }

        // ── Messages d'une conversation ──────────────────────────────────────
        public async Task<List<ConversationMessage>> GetMessagesAsync(string conversationId)
        {
            var raw = await _redis.ListRangeAsync(MessagesKey(conversationId), 0, -1);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var messages = new List<ConversationMessage>();
            foreach (var item in raw)
            {
                try
                {
                    var msg = JsonSerializer.Deserialize<ConversationMessage>(item.ToString()!, opts);
                    if (msg != null) messages.Add(msg);
                }
                catch { /* ignorer les entrées corrompues */ }
            }

            // La liste Redis est stockée dans l'ordre d'insertion (RPUSH) → chronologique
            return messages;
        }

        // ── Ajouter un message ───────────────────────────────────────────────
        public async Task AddMessageAsync(string conversationId, ConversationMessage message)
        {
            // Extraire userId depuis le conversationId (format: "{userId}-{guid}")
            var userId = ExtractUserId(conversationId);
            var json   = JsonSerializer.Serialize(message);
            var now    = DateTime.UtcNow;

            // Ajouter en fin de liste (ordre chronologique)
            await _redis.ListRightPushAsync(MessagesKey(conversationId), json);
            await _redis.KeyExpireAsync(MessagesKey(conversationId), Ttl);

            // Incrémenter le compteur et mettre à jour les métadonnées
            var preview = message.Content.Length > 80
                ? message.Content[..77] + "..."
                : message.Content;

            await _redis.HashSetAsync(MetaKey(conversationId), new HashEntry[]
            {
                new("updatedAt",    now.ToString("O")),
                new("lastMessage",  preview)
            });
            await _redis.HashIncrementAsync(MetaKey(conversationId), "messageCount", 1);
            await _redis.KeyExpireAsync(MetaKey(conversationId), Ttl);

            // Mettre à jour le score dans le sorted set (= date de mise à jour)
            var score = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await _redis.SortedSetAddAsync(UserIndexKey(userId), conversationId, score);
            await _redis.KeyExpireAsync(UserIndexKey(userId), Ttl);

            // Mettre à jour le set global
            await _redis.SortedSetAddAsync(AllConvsSetKey(), $"{userId}:{conversationId}", score);
        }

        // ── Mettre à jour le titre ───────────────────────────────────────────
        public async Task UpdateTitleAsync(string conversationId, string title)
        {
            await _redis.HashSetAsync(MetaKey(conversationId),
                new HashEntry[] { new("title", title) });
            await _redis.KeyExpireAsync(MetaKey(conversationId), Ttl);
        }

        // ── Supprimer une conversation ───────────────────────────────────────
        public async Task DeleteConversationAsync(string conversationId, int userId)
        {
            await _redis.KeyDeleteAsync(MetaKey(conversationId));
            await _redis.KeyDeleteAsync(MessagesKey(conversationId));
            await _redis.SortedSetRemoveAsync(UserIndexKey(userId), conversationId);
            await _redis.SortedSetRemoveAsync(AllConvsSetKey(), $"{userId}:{conversationId}");
        }

        // ── Supprimer toutes les conversations d'un utilisateur ──────────────
        public async Task DeleteAllConversationsAsync(int userId)
        {
            var ids = await _redis.SortedSetRangeByRankAsync(UserIndexKey(userId), 0, -1);
            foreach (var id in ids)
            {
                await _redis.KeyDeleteAsync(MetaKey(id!));
                await _redis.KeyDeleteAsync(MessagesKey(id!));
                await _redis.SortedSetRemoveAsync(AllConvsSetKey(), $"{userId}:{id}");
            }
            await _redis.KeyDeleteAsync(UserIndexKey(userId));
        }

        // ── Purger les conversations expirées (> 30 jours) ───────────────────
        public async Task PurgeExpiredConversationsAsync()
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds();

            // Récupérer tous les membres dont le score (updatedAt) est < cutoff
            var expired = await _redis.SortedSetRangeByScoreAsync(
                AllConvsSetKey(), double.NegativeInfinity, cutoff);

            foreach (var entry in expired)
            {
                var parts = entry.ToString().Split(':', 2);
                if (parts.Length != 2) continue;

                var convId = parts[1];
                if (!int.TryParse(parts[0], out var userId)) continue;

                await _redis.KeyDeleteAsync(MetaKey(convId));
                await _redis.KeyDeleteAsync(MessagesKey(convId));
                await _redis.SortedSetRemoveAsync(UserIndexKey(userId), convId);
                await _redis.SortedSetRemoveAsync(AllConvsSetKey(), entry!);
            }
        }

        // ── Helper ──────────────────────────────────────────────────────────
        private static int ExtractUserId(string conversationId)
        {
            var dash = conversationId.IndexOf('-');
            if (dash > 0 && int.TryParse(conversationId[..dash], out var id))
                return id;
            return 0;
        }
    }
}