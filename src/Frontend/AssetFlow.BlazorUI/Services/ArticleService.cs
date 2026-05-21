using System.Net.Http.Json;

namespace AssetFlow.BlazorUI.Services
{
    public class ArticleService
    {
        private readonly HttpClient _http;
        private const string Base = "api/articles";

        public ArticleService(HttpClient http) => _http = http;

        public async Task<bool> UpdateNumeroSerieAsync(int id, string? numeroSerie)
        {
            var resp = await _http.PatchAsJsonAsync($"{Base}/{id}/numero-serie", new { NumeroSerie = numeroSerie });
            return resp.IsSuccessStatusCode;
        }

        public async Task<(bool Success, string Message)> SupprimerAsync(int id)
        {
            var resp = await _http.DeleteAsync($"{Base}/{id}");
            if (resp.IsSuccessStatusCode) return (true, "Article supprimé.");
            try
            {
                var body = await resp.Content.ReadFromJsonAsync<MsgDto>();
                return (false, body?.Message ?? $"Erreur HTTP {(int)resp.StatusCode}.");
            }
            catch { return (false, $"Erreur HTTP {(int)resp.StatusCode}."); }
        }

        public async Task<(bool Success, string Message)> MettreHorsServiceAsync(int id)
        {
            // PATCH sans body
            var resp = await _http.PatchAsync($"{Base}/{id}/hors-service", null);
            if (resp.IsSuccessStatusCode) return (true, "Article mis hors service.");
            try
            {
                var body = await resp.Content.ReadFromJsonAsync<MsgDto>();
                return (false, body?.Message ?? $"Erreur HTTP {(int)resp.StatusCode}.");
            }
            catch { return (false, $"Erreur HTTP {(int)resp.StatusCode}."); }
        }
        public async Task<(bool Success, string Message)> RemettreEnServiceAsync(int id)
        {
            try
            {
                var resp = await _http.PatchAsync($"api/articles/{id}/remettre-en-service", null);
                if (resp.IsSuccessStatusCode) return (true, "Article remis en service.");
                var body = await resp.Content.ReadAsStringAsync();
                return (false, body);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        private class MsgDto { public string? Message { get; set; } }
    }
}