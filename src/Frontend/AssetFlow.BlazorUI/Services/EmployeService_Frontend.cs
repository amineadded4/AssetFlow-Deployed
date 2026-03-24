// ============================================================
// AssetFlow.BlazorUI / Services / EmployeService.cs  (FICHIER COMPLET)
// ============================================================

using System.Net.Http.Json;
using Microsoft.JSInterop;
using Blazored.LocalStorage;

namespace AssetFlow.BlazorUI.Services
{
    // ═══════════════════════════════════════════════
    // DTOs FRONTEND
    // ═══════════════════════════════════════════════

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
        public string?  NumeroSerie      { get; set; }
        public string   EtatArticle      { get; set; } = "Bon";
    }

    public class ArticleAffecteDto
    {
        public int      AffectationId     { get; set; }
        public int      ArticleId         { get; set; }
        public string   NumeroSerie       { get; set; } = string.Empty;
        public string   StatutArticle     { get; set; } = string.Empty;
        public string   StatutAffectation { get; set; } = string.Empty;
        public string   EtatArticle       { get; set; } = "Bon";
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
        public int      NombreCommentaires  { get; set; } = 0;   // ← NOUVEAU
        public List<ArticleAffecteDto> Articles { get; set; } = new();
    }

    // ── DTOs Commentaire ─────────────────────────────────────
    public class CommentaireDto
    {
        public int      Id              { get; set; }
        public int      MaterielId      { get; set; }
        public int      UtilisateurId   { get; set; }
        public string   AuteurNom       { get; set; } = string.Empty;
        public string   AuteurInitiales { get; set; } = string.Empty;
        public string   Contenu         { get; set; } = string.Empty;
        public DateTime DateCreation    { get; set; }
    }

    public class CreerCommentaireDto
    {
        public int    MaterielId    { get; set; }
        public int    UtilisateurId { get; set; }
        public string Contenu       { get; set; } = string.Empty;
    }

    public class CommentaireResultDto
    {
        public bool   Succes  { get; set; }
        public string Message { get; set; } = string.Empty;
        public int?   Id      { get; set; }
    }

    // ═══════════════════════════════════════════════
    // SERVICE
    // ═══════════════════════════════════════════════

    public class EmployeService
    {
        private readonly HttpClient           _http;
        private readonly IJSRuntime           _js;
        private readonly ILocalStorageService _localStorage;

        public EmployeService(HttpClient http, IJSRuntime js, ILocalStorageService localStorage)
        {
            _http         = http;
            _js           = js;
            _localStorage = localStorage;
        }

        // ── Matériels groupés ─────────────────────────────────
        public async Task<List<MaterielAffecteGroupeDto>> GetMaterielsGroupesAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            try
            {
                var result = await _http.GetFromJsonAsync<List<MaterielAffecteGroupeDto>>(
                    $"api/employe/{userId}/materiels-groupes");
                return result ?? new();
            }
            catch { return new(); }
        }

        // ── Liste plate (rétrocompat) ─────────────────────────
        public async Task<List<EquipementAffecteDto>> GetMesEquipementsAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            try
            {
                var result = await _http.GetFromJsonAsync<List<EquipementAffecteDto>>(
                    $"api/employe/equipements/{userId}");
                return result ?? new();
            }
            catch { return new(); }
        }

        public async Task<EquipementAffecteDto?> GetEquipementDetailAsync(int affectationId, int articleId = 0)
        {
            return await _http.GetFromJsonAsync<EquipementAffecteDto>(
                $"api/employe/equipements/detail/{affectationId}?articleId={articleId}");
        }

        // ── Commentaires ──────────────────────────────────────

        /// <summary>Envoie un commentaire sur un matériel</summary>
        public async Task<CommentaireResultDto> AjouterCommentaireAsync(int materielId, string contenu)
        {
            var userId = await GetCurrentUserIdAsync();
            var dto = new CreerCommentaireDto
            {
                MaterielId    = materielId,
                UtilisateurId = userId,
                Contenu       = contenu
            };
            try
            {
                var response = await _http.PostAsJsonAsync("api/commentaire", dto);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CommentaireResultDto>();
                    return result ?? new CommentaireResultDto { Succes = false, Message = "Réponse vide." };
                }
                var erreur = await response.Content.ReadAsStringAsync();
                return new CommentaireResultDto { Succes = false, Message = erreur };
            }
            catch (Exception ex)
            {
                return new CommentaireResultDto { Succes = false, Message = ex.Message };
            }
        }

        /// <summary>Récupère tous les commentaires d'un matériel</summary>
        public async Task<List<CommentaireDto>> GetCommentairesMaterielAsync(int materielId)
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<CommentaireDto>>(
                    $"api/commentaire/materiel/{materielId}");
                return result ?? new();
            }
            catch { return new(); }
        }

        // ── Helpers localStorage ──────────────────────────────
        public async Task<int> GetCurrentUserIdAsync()
        {
            try
            {
                var id = await _js.InvokeAsync<string>("localStorage.getItem", "user_id");
                return int.TryParse(id, out var parsed) ? parsed : 1;
            }
            catch { return 1; }
        }

        public async Task<string> GetCurrentUserNameAsync()
        {
            try
            {
                var name = await _localStorage.GetItemAsync<string>("user_name");
                return string.IsNullOrEmpty(name) ? "Utilisateur" : name;
            }
            catch { return "Utilisateur"; }
        }

        public async Task<string> GetCurrentUserRoleAsync()
        {
            try
            {
                var role = await _localStorage.GetItemAsync<string>("user_role");
                return string.IsNullOrEmpty(role) ? "Employé" : role;
            }
            catch { return "Employé"; }
        }
    }
}
