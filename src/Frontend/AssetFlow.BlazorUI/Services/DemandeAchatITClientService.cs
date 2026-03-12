// ============================================================
// AssetFlow.BlazorUI / Services / DemandeAchatITClientService.cs
// ============================================================

using System.Net.Http.Json;
using AssetFlow.Application.DTOs;

namespace AssetFlow.BlazorUI.Services
{
    public class DemandeAchatITClientService
    {
        private readonly HttpClient _http;

        public DemandeAchatITClientService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>Récupère toutes les demandes d'achat de l'utilisateur IT.</summary>
        public async Task<List<DemandeAchatITDto>> GetDemandesAsync()
        {
            var result = await _http.GetFromJsonAsync<List<DemandeAchatITDto>>("api/it/demandesachat");
            return result ?? new List<DemandeAchatITDto>();
        }

        /// <summary>Soumet une nouvelle demande d'achat.</summary>
        public async Task CreateDemandeAsync(CreateDemandeAchatDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/it/demandesachat", dto);
            response.EnsureSuccessStatusCode();
        }
    }
}
