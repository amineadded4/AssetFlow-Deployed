// src/Frontend/AssetFlow.BlazorUI/Services/ConversationService.cs
using System.Net.Http.Json;
using AssetFlow.BlazorUI.DTOs;

namespace AssetFlow.BlazorUI.Services
{
    public class ConversationService
    {
        private readonly HttpClient _http;
        private const string Base = "api/conversations";

        public ConversationService(HttpClient http) => _http = http;

        // ── Créer une conversation ───────────────────────────────────────────
        public async Task<ConversationCreatedDto?> CreateAsync(int userId, string title = "Nouvelle conversation")
        {
            try
            {
                var resp = await _http.PostAsJsonAsync(Base, new { userId, title });
                return await resp.Content.ReadFromJsonAsync<ConversationCreatedDto>();
            }
            catch { return null; }
        }

        // ── Liste des conversations ──────────────────────────────────────────
        public async Task<List<ConversationSummary>> GetListAsync(int userId)
        {
            try
            {
                var resp = await _http.GetFromJsonAsync<ConversationListResponseDto>($"{Base}?userId={userId}");
                return resp?.Conversations ?? new();
            }
            catch { return new(); }
        }

        // ── Messages d'une conversation ──────────────────────────────────────
        public async Task<List<ConversationMessageDto>> GetMessagesAsync(string conversationId)
        {
            try
            {
                var resp = await _http.GetFromJsonAsync<ConversationMessagesResponseDto>(
                    $"{Base}/{conversationId}/messages");
                return resp?.Messages ?? new();
            }
            catch { return new(); }
        }

        // ── Ajouter un message ───────────────────────────────────────────────
        // MODIFIÉ — nouveau paramètre optionnel `offersJson` pour persister
        // les cartes d'offres (JSON sérialisé de List<MaterielOffersGroupDto>)
        // dans l'historique Redis de la conversation.
        public async Task AddMessageAsync(string conversationId, string role, string content,
            string? agentUsed = null, string? actionJson = null, bool actionProcessed = false,
            string? offersJson = null)
        {
            try
            {
                await _http.PostAsJsonAsync($"{Base}/{conversationId}/messages", new
                {
                    conversationId,
                    role,
                    content,
                    agentUsed,
                    actionJson,
                    actionProcessed,
                    offersJson    // ← NOUVEAU
                });
            }
            catch { /* silencieux — la conversation locale continue */ }
        }

        // ── Renommer ────────────────────────────────────────────────────────
        public async Task UpdateTitleAsync(string conversationId, string title)
        {
            try
            {
                await _http.PutAsJsonAsync($"{Base}/{conversationId}/title", new { title });
            }
            catch { }
        }

        // ── Supprimer une conversation ───────────────────────────────────────
        public async Task DeleteAsync(string conversationId, int userId)
        {
            try
            {
                await _http.DeleteAsync($"{Base}/{conversationId}?userId={userId}");
            }
            catch { }
        }

        // ── Supprimer toutes les conversations ───────────────────────────────
        public async Task DeleteAllAsync(int userId)
        {
            try
            {
                await _http.DeleteAsync($"{Base}/all?userId={userId}");
            }
            catch { }
        }
    }
}
