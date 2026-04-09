using System.Net.Http.Json;
using AssetFlow.BlazorUI.DTOs;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Services
{
    public class DemandeAchatITClientService
    {
        private readonly HttpClient _http;
        [Inject] private ILocalStorageService        LocalStorage     { get; set; } = default!;
        private const string Base = "api/it/demandesachat";

        public DemandeAchatITClientService(HttpClient http) => _http = http;

        // ── userId passé en query param pour filtrer côté API ────
        public async Task<List<DemandeAchatITDto>> GetDemandesAsync(int? userId = null)
        {
            var url = userId.HasValue ? $"{Base}?userId={userId.Value}" : Base;
            var result = await _http.GetFromJsonAsync<List<DemandeAchatITDto>>(url);
            return result ?? new();
        }

        public async Task CreateDemandeAsync(CreateDemandeAchatDto dto)
        {
            var response = await _http.PostAsJsonAsync(Base, dto);
            response.EnsureSuccessStatusCode();
        }

        public async Task<DemandeAchatITDto?> UpdateDemandeAsync(int id, UpdateDemandeAchatDto dto)
        {
            var response = await _http.PutAsJsonAsync($"{Base}/{id}", dto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DemandeAchatITDto>();
        }

        public async Task DeleteDemandeAsync(int id)
        {
            var userName  = await LocalStorage.GetItemAsync<string>("user_name")  ?? "Inconnu";

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{Base}/{id}");
            request.Headers.Add("X-User-Name", userName);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}