using AssetFlow.Application.DTOs;
using System.Net.Http.Json;

namespace AssetFlow.BlazorUI.Services
{
    public class OffreDemandeService
    {
        private readonly HttpClient _http;

        public OffreDemandeService(HttpClient http) => _http = http;

        // ── Offres ───────────────────────────────────────────────────────────

        public async Task<List<OffreAchatDto>> GetOffresByDemandeAsync(int demandeId)
            => await _http.GetFromJsonAsync<List<OffreAchatDto>>($"api/offreachat/demande/{demandeId}") ?? new();

        public async Task<byte[]> GetPdfBytesAsync(Guid offreId)
            => await _http.GetByteArrayAsync($"api/offreachat/{offreId}/pdf");

        // ── OCR ──────────────────────────────────────────────────────────────

        public async Task<InvoiceOcrDto?> GetOcrCacheAsync(Guid offreId)
        {
            var response = await _http.GetAsync($"api/ocr/cache/{offreId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<InvoiceOcrDto>();
        }

        public async Task<(InvoiceOcrDto? Invoice, string? Error)> AnalyzeOcrAsync(Guid offreId)
        {
            var response = await _http.PostAsync($"api/ocr/analyze/{offreId}", null);
            if (!response.IsSuccessStatusCode)
                return (null, await response.Content.ReadAsStringAsync());
            var invoice = await response.Content.ReadFromJsonAsync<InvoiceOcrDto>();
            return (invoice, null);
        }

        // ── Confirmation ─────────────────────────────────────────────────────

        public async Task<(bool Success, string? Error)> ConfirmOffreAsync(object payload)
        {
            var response = await _http.PostAsJsonAsync("api/offre-selection/confirm", payload);
            if (!response.IsSuccessStatusCode)
                return (false, $"Erreur : {response.StatusCode}");
            return (true, null);
        }

        // ── Chat ─────────────────────────────────────────────────────────────

        public async Task<ChatResponseDto?> SendChatMessageAsync(object payload)
        {
            var response = await _http.PostAsJsonAsync("api/chat-offre/send", payload);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ChatResponseDto>();
        }

        public async Task<List<ChatbotMessageDto>> GetChatHistoryAsync(string userId, int demandeId)
            => await _http.GetFromJsonAsync<List<ChatbotMessageDto>>($"api/chat-offre/history/{userId}/{demandeId}") ?? new();

        public async Task<ChatRecommendationDto?> GetRecommendationAsync(string userId, int demandeId)
            => await _http.GetFromJsonAsync<ChatRecommendationDto>($"api/chat-offre/recommendation/{userId}/{demandeId}");
    }
}