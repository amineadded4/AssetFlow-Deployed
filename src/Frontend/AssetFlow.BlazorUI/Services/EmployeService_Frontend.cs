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
        public int      NombreCommentaires  { get; set; } = 0;
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

        public class CommentaireITDto
        {
            public int      Id                { get; set; }
            public int      MaterielId        { get; set; }
            public string   MaterielRef       { get; set; } = string.Empty;
            public string   MaterielNom       { get; set; } = string.Empty;
            public string   MaterielCategorie { get; set; } = string.Empty;
            public int      UtilisateurId     { get; set; }
            public string   AuteurNom         { get; set; } = string.Empty;
            public string   AuteurInitiales   { get; set; } = string.Empty;
            public string   AuteurRole        { get; set; } = string.Empty;
            public string   Contenu           { get; set; } = string.Empty;
            public DateTime DateCreation      { get; set; }
        }

        public class SentimentMaterielDto
        {
            public int    MaterielId          { get; set; }
            public string MaterielRef         { get; set; } = string.Empty;
            public string MaterielNom         { get; set; } = string.Empty;
            public int    TotalCommentaires   { get; set; }
            public int    Positifs            { get; set; }
            public int    Negatifs            { get; set; }
            public int    Neutres             { get; set; }
            public double PourcentagePositif  { get; set; }
            public double PourcentageNegatif  { get; set; }
            public double PourcentageNeutre   { get; set; }
            public string Resume              { get; set; } = string.Empty;
            public double ScoreGlobal         { get; set; }
            public string SentimentDominant   { get; set; } = string.Empty;
        }

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

        // Envoie un commentaire sur un matériel
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

        // Récupère les commentaires d'un utilisateur pour un matériel
        public async Task<List<CommentaireDto>> GetCommentairesMaterielAsync(int materielId)
        {
            var userId = await GetCurrentUserIdAsync();
            try
            {
                var result = await _http.GetFromJsonAsync<List<CommentaireDto>>(
                    $"api/commentaire/materiel/{materielId}/{userId}");
                return result ?? new();
            }
            catch { return new(); }
        }

        // Supprime un commentaire (seulement par son auteur)
        public async Task<CommentaireResultDto> SupprimerCommentaireAsync(int commentaireId)
        {
            var userId = await GetCurrentUserIdAsync();
            try
            {
                var response = await _http.DeleteAsync($"api/commentaire/{commentaireId}/{userId}");
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
        
        // Analyse sentiment d'un matériel via l'IA (HuggingFace gratuit) 
        public async Task<SentimentMaterielDto?> GetSentimentMaterielAsync(int materielId)
        {
            try
            {
                return await _http.GetFromJsonAsync<SentimentMaterielDto>(
                    $"api/sentiment/materiel/{materielId}");
            }
            catch { return null; }
        }
        
        // Analyse sentiment de tous les matériels commentés
        public async Task<List<SentimentMaterielDto>> GetSentimentTousAsync()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<SentimentMaterielDto>>("api/sentiment/tous");
                return result ?? new();
            }
            catch { return new(); }
        }
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
                // ── Méthode à ajouter dans la classe EmployeService ──────────
        /// <summary>Vue IT : récupère tous les commentaires, filtrables par référence</summary>
        public async Task<List<CommentaireITDto>> GetTousLesCommentairesAsync(string? reference = null)
        {
            try
            {
                var url = string.IsNullOrWhiteSpace(reference)
                    ? "api/commentaire/it/tous"
                    : $"api/commentaire/it/tous?reference={Uri.EscapeDataString(reference)}";
        
                var result = await _http.GetFromJsonAsync<List<CommentaireITDto>>(url);
                return result ?? new();
            }
            catch { return new(); }
        }
        // Suppression admin IT — sans vérification d'auteur
        public async Task<CommentaireResultDto> SupprimerCommentaireITAsync(int commentaireId)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/commentaire/admin/{commentaireId}");
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
    }
}