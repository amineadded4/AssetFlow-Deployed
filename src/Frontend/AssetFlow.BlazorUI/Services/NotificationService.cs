using System.Net.Http.Json;
using AssetFlow.BlazorUI.DTOs;

namespace AssetFlow.BlazorUI.Services
{
    public class NotificationService
    {
        private readonly HttpClient _http;
        private const string Base = "api/notifications";

        public NotificationService(HttpClient http) => _http = http;

        public async Task<List<NotificationDto>> GetNotificationsAsync(bool nonLuesSeulement = false)
        {
            try
            {
                var url = nonLuesSeulement ? $"{Base}?nonLuesSeulement=true" : Base;
                return await _http.GetFromJsonAsync<List<NotificationDto>>(url) ?? new();
            }
            catch { return new(); }
        }

        public async Task<int> GetNombreNonLuesAsync()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<CountDto>($"{Base}/count");
                return result?.NombreNonLues ?? 0;
            }
            catch { return 0; }
        }

        public async Task MarquerCommeLueAsync(int id)
        {
            try { await _http.PatchAsync($"{Base}/{id}/lue", null); }
            catch { }
        }

        public async Task MarquerToutesCommeLuesAsync()
        {
            try { await _http.PatchAsync($"{Base}/tout-lire", null); }
            catch { }
        }

        private class CountDto
        {
            public int NombreNonLues { get; set; }
        }
    }
}