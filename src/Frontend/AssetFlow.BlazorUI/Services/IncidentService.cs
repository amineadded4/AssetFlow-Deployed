using System.Net.Http.Json;
using Blazored.LocalStorage;

namespace AssetFlow.BlazorUI.Services
{
    // DTO pour signaler un incident (request)
    public class SignalerIncidentRequestDto
    {
        public int AffectationId { get; set; }
        public int? ArticleId { get; set; }
        public string TypeIncident { get; set; } = string.Empty;
        public int Urgence { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    // DTO pour la réponse après signalement
    public class SignalerIncidentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IncidentId { get; set; }
        public string? NumeroIncident { get; set; }
    }

    // DTO représentant un incident
    public class IncidentDto
    {
        public int Id { get; set; }
        public int AffectationId { get; set; }
        public string NumeroIncident { get; set; } = string.Empty;
        public string TypeIncident { get; set; } = string.Empty;
        public int Urgence { get; set; }
        public string UrgenceLabel { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DateIncident { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string StatutLabel { get; set; } = string.Empty;
        public DateTime? DateResolution { get; set; }
        public string? CommentairesResolution { get; set; }
        public string MaterielDesignation { get; set; } = string.Empty;
        public string MaterielReference { get; set; } = string.Empty;
    }

    // Service pour gérer les incidents côté frontend
    public class IncidentService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;

        public IncidentService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        // Signale un nouvel incident
        public async Task<SignalerIncidentResponseDto> SignalerIncidentAsync(SignalerIncidentRequestDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/incident/signaler", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SignalerIncidentResponseDto>();
                    return result ?? new SignalerIncidentResponseDto
                    {
                        Success = false,
                        Message = "Erreur lors de la désérialisation"
                    };
                }

                return new SignalerIncidentResponseDto
                {
                    Success = false,
                    Message = "Erreur lors du signalement"
                };
            }
            catch (Exception ex)
            {
                return new SignalerIncidentResponseDto
                {
                    Success = false,
                    Message = $"Erreur : {ex.Message}"
                };
            }
        }

        // Récupère tous les incidents liés à une affectation spécifique
        public async Task<List<IncidentDto>> GetIncidentsByAffectationAsync(int affectationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/incident/affectation/{affectationId}");

                if (response.IsSuccessStatusCode)
                {
                    var incidents = await response.Content.ReadFromJsonAsync<List<IncidentDto>>();
                    return incidents ?? new List<IncidentDto>();
                }

                // Fallback : récupérer tous les incidents de l'utilisateur et filtrer
                var userId = await _localStorage.GetItemAsync<int?>("user_id");
                if (userId == null)
                    return new List<IncidentDto>();

                var allResponse = await _httpClient.GetAsync($"api/incident/utilisateur/{userId}");
                if (allResponse.IsSuccessStatusCode)
                {
                    var all = await allResponse.Content.ReadFromJsonAsync<List<IncidentDto>>();
                    return all?.Where(i => i.AffectationId == affectationId).ToList()
                           ?? new List<IncidentDto>();
                }

                return new List<IncidentDto>();
            }
            catch
            {
                return new List<IncidentDto>();
            }
        }

        // Récupère le détail d'un incident
        public async Task<IncidentDto?> GetIncidentDetailAsync(int incidentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/incident/{incidentId}");

                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<IncidentDto>();

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}