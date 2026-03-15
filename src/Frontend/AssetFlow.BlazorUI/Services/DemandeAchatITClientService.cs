// ============================================================
// AssetFlow.BlazorUI / Services / DemandeAchatITClientService.cs
// AJOUT : UpdateDemandeAsync + DeleteDemandeAsync
// ============================================================

using System.Net.Http.Json;
using AssetFlow.Application.DTOs;

namespace AssetFlow.BlazorUI.Services
{
    public class DemandeAchatITClientService
    {
        private readonly HttpClient _http;
        private const string Base = "api/it/demandesachat";

        public DemandeAchatITClientService(HttpClient http) => _http = http;

        public async Task<List<DemandeAchatITDto>> GetDemandesAsync()
        {
            var result = await _http.GetFromJsonAsync<List<DemandeAchatITDto>>(Base);
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
            var response = await _http.DeleteAsync($"{Base}/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}