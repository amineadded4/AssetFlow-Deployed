using AssetFlow.Application.DTOs;
using System.Net.Http.Json;

namespace AssetFlow.BlazorUI.Services
{
    public class StatistiquesService
    {
        private readonly HttpClient _http;
        public StatistiquesService(HttpClient http) => _http = http;

        public async Task<DashboardStatsDto?> GetDashboardAsync(
            int annee, int moisDebut = 1, int moisFin = 12)
        {
            try
            {
                return await _http.GetFromJsonAsync<DashboardStatsDto>(
                    $"api/statistiques?annee={annee}&moisDebut={moisDebut}&moisFin={moisFin}");
            }
            catch
            {
                return null;
            }
        }
    }
}
