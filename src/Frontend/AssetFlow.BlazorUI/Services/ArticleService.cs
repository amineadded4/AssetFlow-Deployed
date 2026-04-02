using System.Net.Http.Json;

namespace AssetFlow.BlazorUI.Services
{
    public class ArticleService
    {
        private readonly HttpClient _http;
        private const string Base = "api/articles";

        public ArticleService(HttpClient http) => _http = http;

        // Met à jour le numéro de série d'un article individuel
        public async Task<bool> UpdateNumeroSerieAsync(int articleId, string? numeroSerie)
        {
            var payload = new { NumeroSerie = string.IsNullOrWhiteSpace(numeroSerie) ? null : numeroSerie.Trim() };
            var resp = await _http.PatchAsJsonAsync($"{Base}/{articleId}/numero-serie", payload);
            return resp.IsSuccessStatusCode;
        }
    }
}