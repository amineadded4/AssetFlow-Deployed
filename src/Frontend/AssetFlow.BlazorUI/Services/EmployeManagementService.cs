using System.Net.Http.Json;

namespace AssetFlow.BlazorUI.Services
{
    public class EmployeListeDto
    {
        public int      Id                    { get; set; }
        public string   FullName              { get; set; } = string.Empty;
        public string   Email                 { get; set; } = string.Empty;
        public string   Role            { get; set; } = string.Empty;
        public string   Initials              { get; set; } = string.Empty;
        public int      NbAffectationsActives { get; set; }
        public DateTime CreatedAt             { get; set; }
    }

    public class ArticleAffectationDto
    {
        public int    ArticleId   { get; set; }
        public string NumeroSerie { get; set; } = string.Empty;
        public string Etat        { get; set; } = string.Empty;
    }

    public class AffectationEmployeDto
    {
        public int       AffectationId     { get; set; }
        public int       MaterielId        { get; set; }
        public string    Designation       { get; set; } = string.Empty;
        public string    Reference         { get; set; } = string.Empty;
        public string    Categorie         { get; set; } = string.Empty;
        public string?   ImageUrl          { get; set; }
        public DateTime  DateAffectation   { get; set; }
        public DateTime? DateRetourPrevue  { get; set; }
        public string    Etat              { get; set; } = string.Empty;
        public string?   Observations      { get; set; }
        public List<ArticleAffectationDto> Articles { get; set; } = new();
    }

    public class ProjetAffectationListeDto
    {
        public int    Id          { get; set; }
        public string Nom         { get; set; } = string.Empty;
        public string Statut      { get; set; } = string.Empty;
        public string Priorite    { get; set; } = string.Empty;
        public string? Responsable { get; set; }
        public int    NbAffectationsActives { get; set; }
    }

    public class EmployeManagementService
    {
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
                var resp   = await _http.DeleteAsync($"{Base}/affectations/{affectationId}");
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