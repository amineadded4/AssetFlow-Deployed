using System.Net.Http.Json;
using AssetFlow.BlazorUI.DTOs;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
namespace AssetFlow.BlazorUI.Services
{

    public class EmployeManagementService
    {
        [Inject] private ILocalStorageService        LocalStorage     { get; set; } = default!;
        private readonly HttpClient _http;
        private const string Base = "api/employes";

        public EmployeManagementService(HttpClient http) => _http = http;

        // ── Employés ──
        public async Task<List<EmployeListeDto>> GetEmployesAsync(string? search = null)
        {
            var url = string.IsNullOrWhiteSpace(search) ? Base : $"{Base}?search={Uri.EscapeDataString(search)}";
            try { return await _http.GetFromJsonAsync<List<EmployeListeDto>>(url) ?? new(); }
            catch { return new(); }
        }

        public async Task<List<AffectationEmployeDto>> GetAffectationsAsync(int userId)
        {
            try { return await _http.GetFromJsonAsync<List<AffectationEmployeDto>>($"{Base}/{userId}/affectations") ?? new(); }
            catch { return new(); }
        }

        // ── Projets avec affectations ──
        public async Task<List<ProjetAffectationListeDto>> GetProjetsAvecAffectationsAsync(string? search = null)
        {
            // Récupère les projets depuis api/affectation/projets
            var url = string.IsNullOrWhiteSpace(search)
                ? "api/affectation/projets"
                : $"api/affectation/projets?search={Uri.EscapeDataString(search)}";
            try
            {
                var projets = await _http.GetFromJsonAsync<List<ProjetDisponibleDto>>(url) ?? new();
                // Enrichir avec le nb d'affectations actives
                var result = new List<ProjetAffectationListeDto>();
                foreach (var p in projets)
                {
                    var affs = await GetAffectationsProjetAsync(p.Id);
                    result.Add(new ProjetAffectationListeDto
                    {
                        Id                    = p.Id,
                        Nom                   = p.Nom,
                        Statut                = p.Statut,
                        Priorite              = p.Priorite,
                        Responsable           = p.Responsable,
                        NbAffectationsActives = affs.Count(a => a.Etat == "Courante")
                    });
                }
                return result;
            }
            catch { return new(); }
        }

        public async Task<List<AffectationEmployeDto>> GetAffectationsProjetAsync(int projetId)
        {
            try { return await _http.GetFromJsonAsync<List<AffectationEmployeDto>>($"api/projects/{projetId}/affectations") ?? new(); }
            catch { return new(); }
        }

        // ── Révoquer (identique pour user et projet) ──
        public async Task<(bool Succes, string Message)> RetirerAffectationAsync(int affectationId)
        {
            try
            {
                var userName = await LocalStorage.GetItemAsync<string>("user_name") ?? "Inconnu";

                var request = new HttpRequestMessage(HttpMethod.Delete, $"{Base}/affectations/{affectationId}");
                request.Headers.Add("X-User-Name", userName);

                var resp   = await _http.SendAsync(request);
                var result = await resp.Content.ReadFromJsonAsync<RetirerAffectationResultDto>();
                return (result?.Succes ?? false, result?.Message ?? "Erreur");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
    }

    public class RetirerAffectationResultDto
    {
        public bool   Succes  { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}