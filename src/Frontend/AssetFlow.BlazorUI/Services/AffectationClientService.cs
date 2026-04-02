using System.Net.Http.Json;

namespace AssetFlow.BlazorUI.Services
{
    public class UtilisateurDisponibleDto
    {
        public int    Id         { get; set; }
        public string FullName   { get; set; } = string.Empty;
        public string Email      { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Initials   { get; set; } = string.Empty;
    }

    public class ArticleDisponibleDto
    {
        public int    Id          { get; set; }
        public string NumeroSerie { get; set; } = string.Empty;
        public string Etat        { get; set; } = "Bon";
    }

    public class MaterielDisponibleDto
    {
        public int    Id                 { get; set; }
        public string Reference          { get; set; } = string.Empty;
        public string Designation        { get; set; } = string.Empty;
        public string Categorie          { get; set; } = string.Empty;
        public string? ImageUrl          { get; set; }
        public int    QuantiteDisponible { get; set; }
        public List<ArticleDisponibleDto> Articles { get; set; } = new();
    }

    public class ProjetDisponibleDto
    {
        public int    Id           { get; set; }
        public string Nom          { get; set; } = string.Empty;
        public string Statut       { get; set; } = string.Empty;
        public string Priorite     { get; set; } = string.Empty;
        public string? Responsable { get; set; }
    }

    public class CreerAffectationRequest
    {
        public int       MaterielId       { get; set; }
        public int?       UtilisateurId    { get; set; }
        public List<int> ArticleIds       { get; set; } = new();
        public string?   Observations     { get; set; }
        public DateTime? DateRetourPrevue { get; set; }
        public int?      ProjetId         { get; set; }
    }

    public class AffectationResultDto
    {
        public bool   Succes        { get; set; }
        public string Message       { get; set; } = string.Empty;
        public int    AffectationId { get; set; }
    }

    public class AffectationClientService
    {
        private readonly HttpClient _http;
        private const string Base = "api/affectation";

        public AffectationClientService(HttpClient http) => _http = http;

        public async Task<List<UtilisateurDisponibleDto>> GetUtilisateursAsync(string? search = null)
        {
            var url = string.IsNullOrWhiteSpace(search)
                ? $"{Base}/utilisateurs"
                : $"{Base}/utilisateurs?search={Uri.EscapeDataString(search)}";
            try { return await _http.GetFromJsonAsync<List<UtilisateurDisponibleDto>>(url) ?? new(); }
            catch { return new(); }
        }

        public async Task<List<MaterielDisponibleDto>> GetMaterielsAsync(string? search = null)
        {
            var url = string.IsNullOrWhiteSpace(search)
                ? $"{Base}/materiels"
                : $"{Base}/materiels?search={Uri.EscapeDataString(search)}";
            try { return await _http.GetFromJsonAsync<List<MaterielDisponibleDto>>(url) ?? new(); }
            catch { return new(); }
        }

        public async Task<List<ProjetDisponibleDto>> GetProjetsAsync(string? search = null)
        {
            var url = string.IsNullOrWhiteSpace(search)
                ? $"{Base}/projets"
                : $"{Base}/projets?search={Uri.EscapeDataString(search)}";
            try { return await _http.GetFromJsonAsync<List<ProjetDisponibleDto>>(url) ?? new(); }
            catch { return new(); }
        }

        public async Task<AffectationResultDto> CreerAffectationAsync(CreerAffectationRequest request)
        {
            try
            {
                var resp   = await _http.PostAsJsonAsync(Base, request);
                var result = await resp.Content.ReadFromJsonAsync<AffectationResultDto>();
                return result ?? new AffectationResultDto { Succes = false, Message = "Réponse vide." };
            }
            catch (Exception ex)
            {
                return new AffectationResultDto { Succes = false, Message = $"Erreur réseau: {ex.Message}" };
            }
        }
    }
}