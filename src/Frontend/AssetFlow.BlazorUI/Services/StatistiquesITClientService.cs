using AssetFlow.Application.DTOs;
using System.Net.Http.Json;

namespace AssetFlow.BlazorUI.Services
{
    public class StatistiquesITService
    {
        private readonly HttpClient _http;

        public StatistiquesITService(HttpClient http) => _http = http;

        public async Task<DashboardITStatsDto?> GetDashboardAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<DashboardITStatsDto>("api/statistiques-it");
            }
            catch
            {
                return null;
            }
        }
    }
}
