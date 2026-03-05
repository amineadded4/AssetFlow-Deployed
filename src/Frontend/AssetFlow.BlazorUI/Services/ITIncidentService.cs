using System.Net.Http.Json;

namespace AssetFlow.BlazorUI.Services
{
    public class IncidentEmployeDto
    {
        public int    UtilisateurId     { get; set; }
        public string FullName          { get; set; } = string.Empty;
        public string Department        { get; set; } = string.Empty;
        public string Initials          { get; set; } = string.Empty;
        public int    NbIncidentsActifs { get; set; }
    }

    public class IncidentItemDto
    {
        public int      Id                      { get; set; }
        public int      AffectationId           { get; set; }
        public string   NumeroIncident          { get; set; } = string.Empty;
        public string   TypeIncident            { get; set; } = string.Empty;
        public int      Urgence                 { get; set; }
        public string   UrgenceLabel            { get; set; } = string.Empty;
        public string   Description             { get; set; } = string.Empty;
        public DateTime DateIncident            { get; set; }
        public string   Statut                  { get; set; } = string.Empty;
        public string   StatutLabel             { get; set; } = string.Empty;
        public DateTime? DateResolution         { get; set; }
        public string?  CommentairesResolution  { get; set; }
    }

    public class IncidentArticleDto
    {
        public int    ArticleId    { get; set; }
        public string NumeroSerie  { get; set; } = string.Empty;
        public string EtatArticle  { get; set; } = string.Empty;
        public List<IncidentItemDto> Incidents { get; set; } = new();
    }

    public class IncidentMaterielDto
    {
        public int    MaterielId         { get; set; }
        public int    AffectationId      { get; set; }
        public string Designation        { get; set; } = string.Empty;
        public string Reference          { get; set; } = string.Empty;
        public string? ImageUrl          { get; set; }
        public string Categorie          { get; set; } = string.Empty;
        public int    NbIncidentsActifs  { get; set; }
        public List<IncidentArticleDto> Articles { get; set; } = new();
    }

    public class ITIncidentService
    {
        private readonly HttpClient _http;
        private const string Base = "api/it/incidents";

        public ITIncidentService(HttpClient http) => _http = http;

        public async Task<List<IncidentEmployeDto>> GetEmployesAsync(string? search = null)
        {
            var url = string.IsNullOrWhiteSpace(search) ? $"{Base}/employes"
                : $"{Base}/employes?search={Uri.EscapeDataString(search)}";
            try { return await _http.GetFromJsonAsync<List<IncidentEmployeDto>>(url) ?? new(); }
            catch { return new(); }
        }

        public async Task<List<IncidentMaterielDto>> GetMaterielsAsync(int userId)
        {
            try { return await _http.GetFromJsonAsync<List<IncidentMaterielDto>>($"{Base}/employes/{userId}/materiels") ?? new(); }
            catch { return new(); }
        }

        public async Task<(bool Ok, string Msg)> ChangerStatutAsync(int incidentId, string statut, string? commentaire = null)
        {
            try
            {
                var resp = await _http.PatchAsJsonAsync($"{Base}/{incidentId}/statut",
                    new { NouveauStatut = statut, CommentairesResolution = commentaire });
                var r = await resp.Content.ReadFromJsonAsync<SignalerIncidentResult>();
                return (r?.Success ?? false, r?.Message ?? "Erreur");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool Ok, string Msg)> ResolveAllByArticleAsync(int articleId, string? commentaire = null)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync($"{Base}/resolve-all-article",
                    new { ArticleId = articleId, CommentairesResolution = commentaire });
                var r = await resp.Content.ReadFromJsonAsync<SignalerIncidentResult>();
                return (r?.Success ?? false, r?.Message ?? "Erreur");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
    }

    public class SignalerIncidentResult
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}