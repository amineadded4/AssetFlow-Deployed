// ============================================================
// AssetFlow.BlazorUI / Services / EmployeService.cs
// MISE À JOUR : ajout GetMaterielsGroupesAsync
// ============================================================

using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Services
{
    // ── DTOs frontend (miroir du backend) ─────────────────────

    public class EquipementAffecteDto
    {
        public int      AffectationId    { get; set; }
        public int      MaterielId       { get; set; }
        public string   Reference        { get; set; } = string.Empty;
        public string   Designation      { get; set; } = string.Empty;
        public string   Categorie        { get; set; } = string.Empty;
        public string?  ImageUrl         { get; set; }
        public DateTime DateAffectation  { get; set; }
        public int      QuantiteAffectee { get; set; }
        public string   Statut           { get; set; } = string.Empty;
        public string   StatutBadgeColor { get; set; } = string.Empty;
        public string?  Observations     { get; set; }
    }

    public class ArticleAffecteDto
    {
        public int      AffectationId     { get; set; }
        public int      ArticleId         { get; set; }
        public string   NumeroSerie       { get; set; } = string.Empty;
        public string   StatutArticle     { get; set; } = string.Empty;
        public string   StatutAffectation { get; set; } = string.Empty;
        public string   StatutBadgeColor  { get; set; } = string.Empty;
        public DateTime DateAffectation   { get; set; }
        public string?  Observations      { get; set; }
    }

    public class MaterielAffecteGroupeDto
    {
        public int      MaterielId          { get; set; }
        public string   Reference           { get; set; } = string.Empty;
        public string   Designation         { get; set; } = string.Empty;
        public string   Categorie           { get; set; } = string.Empty;
        public string?  ImageUrl            { get; set; }
        public int      NombreArticles      { get; set; }
        public string   StatutDominant      { get; set; } = string.Empty;
        public string   StatutBadgeColor    { get; set; } = string.Empty;
        public DateTime DerniereAffectation { get; set; }
        public List<ArticleAffecteDto> Articles { get; set; } = new();
    }

    // ── Service ───────────────────────────────────────────────

    public class EmployeService
    {
        private readonly HttpClient  _http;
        private readonly IJSRuntime  _js;

        public EmployeService(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js   = js;
        }

        // ── Récupère les matériels groupés (NOUVELLE méthode) ──
        public async Task<List<MaterielAffecteGroupeDto>> GetMaterielsGroupesAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            try
            {
                var result = await _http.GetFromJsonAsync<List<MaterielAffecteGroupeDto>>(
                    $"api/employe/{userId}/materiels-groupes");
                return result ?? new List<MaterielAffecteGroupeDto>();
            }
            catch
            {
                return new List<MaterielAffecteGroupeDto>();
            }
        }

        // ── Ancienne méthode liste plate (rétrocompat) ─────────
        public async Task<List<EquipementAffecteDto>> GetMesEquipementsAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            try
            {
                var result = await _http.GetFromJsonAsync<List<EquipementAffecteDto>>(
                    $"api/employe/equipements/{userId}");
                return result ?? new List<EquipementAffecteDto>();
            }
            catch
            {
                return new List<EquipementAffecteDto>();
            }
        }

        public async Task<EquipementAffecteDto?> GetEquipementDetailAsync(int affectationId)
        {
            try
            {
                return await _http.GetFromJsonAsync<EquipementAffecteDto>(
                    $"api/employe/equipements/detail/{affectationId}");
            }
            catch
            {
                return null;
            }
        }

        // ── Helpers localStorage ───────────────────────────────
        public async Task<int> GetCurrentUserIdAsync()
        {
            try
            {
                var id = await _js.InvokeAsync<string>("localStorage.getItem", "userId");
                return int.TryParse(id, out var parsed) ? parsed : 1;
            }
            catch { return 1; }
        }

        public async Task<string> GetCurrentUserNameAsync()
        {
            try
            {
                var name = await _js.InvokeAsync<string>("localStorage.getItem", "userName");
                return string.IsNullOrEmpty(name) ? "Utilisateur" : name;
            }
            catch { return "Utilisateur"; }
        }

        public async Task<string> GetCurrentUserRoleAsync()
        {
            try
            {
                var role = await _js.InvokeAsync<string>("localStorage.getItem", "userRole");
                return string.IsNullOrEmpty(role) ? "Employé" : role;
            }
            catch { return "Employé"; }
        }
    }
}