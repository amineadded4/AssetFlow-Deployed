using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Application.DTOs;

namespace AssetFlow.Infrastructure.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _db;

        public ChatService(AppDbContext db) => _db = db;

        // ── Message texte ────────────────────────────────────────────────────
        public async Task<ChatMessagePayload> SendMessageAsync(
            int senderId, int receiverId, string content)
        {
            var msg = new ChatMessage
            {
                SenderId   = senderId,
                ReceiverId = receiverId,
                Content    = content.Trim(),
                SentAt     = DateTime.UtcNow,
                IsRead     = false
            };

            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            return MapToPayload(msg);
        }

        // ── Message vocal ────────────────────────────────────────────────────
        public async Task<ChatMessagePayload> SendVoiceMessageAsync(
            int senderId, int receiverId, string audioBase64, int durationSeconds)
        {
            // Limiter à 60 secondes côté serveur (sécurité)
            if (durationSeconds > 60) durationSeconds = 60;

            var msg = new ChatMessage
            {
                SenderId             = senderId,
                ReceiverId           = receiverId,
                Content              = string.Empty,      // vide pour les vocaux
                AudioData            = audioBase64,
                AudioDurationSeconds = durationSeconds,
                SentAt               = DateTime.UtcNow,
                IsRead               = false
            };

            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            return MapToPayload(msg);
        }

        // ── Marquer comme lu ─────────────────────────────────────────────────
        public async Task MarkReadAsync(int readerId, int senderId)
        {
            var unread = await _db.ChatMessages
                .Where(m => m.SenderId == senderId && m.ReceiverId == readerId && !m.IsRead)
                .ToListAsync();

            foreach (var m in unread) m.IsRead = true;
            await _db.SaveChangesAsync();
        }

        public Task<List<int>> GetOnlineUsersAsync()
        {
            throw new NotImplementedException("Utiliser IConnectionTracker pour accéder aux users en ligne.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static ChatMessagePayload MapToPayload(ChatMessage msg) => new()
        {
            Id                   = msg.Id,
            SenderId             = msg.SenderId,
            ReceiverId           = msg.ReceiverId,
            Content              = msg.Content,
            SentAt               = msg.SentAt,
            IsRead               = msg.IsRead,
            AudioData            = msg.AudioData,
            AudioDurationSeconds = msg.AudioDurationSeconds
        };
    }
}
