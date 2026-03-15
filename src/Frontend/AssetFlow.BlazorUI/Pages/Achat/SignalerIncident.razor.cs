// ============================================================
// Pages/Employe/SignalerIncident.razor.cs
//
// MODIFICATIONS PAR RAPPORT À L'ORIGINAL :
//   1. Route optionnelle : AffectationId n'est plus obligatoire
//   2. OnInitializedAsync charge GetMesEquipementsAsync()
//   3. Si AffectationId est fourni dans l'URL → pré-sélection dans la dropdown
//   4. La validation vérifie que SelectedAffectationId > 0
//
// AUCUN changement dans le design ou la logique de soumission.
// ============================================================

using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class SignalerIncident
    {
        // ── Paramètre URL optionnel ────────────────────────────
        // Renseigné depuis DetailsEquipement via NaviguerVersSignalement()
        // Vaut 0 si on arrive depuis /employe/incident (sidebar)
        [Parameter] public int AffectationId { get; set; } = 0;
        [Parameter] public int ArticleId { get; set; } = 0;
        private List<ArticleAffecteDto> Articles { get; set; } = new();
        private List<MaterielAffecteGroupeDto> Groupes { get; set; } = new();
        private int SelectedArticleId { get; set; } = 0;

        // ── Injections ─────────────────────────────────────────
        [Inject] private IncidentService IncidentService { get; set; } = default!;
        [Inject] private EmployeService EmployeService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        // ── Formulaire ──────────────────────────────────────────
        private string TypeIncident { get; set; } = "Panne";
        private string Description { get; set; } = string.Empty;
        private int Urgence { get; set; } = 50;

        // ── États ───────────────────────────────────────────────
        private bool IsLoading { get; set; } = true;
        private bool IsSubmitting { get; set; } = false;
        private string ErrorMessage { get; set; } = string.Empty;

        // ── Infos utilisateur ──────────────────────────────────
        private string UserName { get; set; } = "Utilisateur";

        // ── Initialisation ─────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            UserName = await EmployeService.GetCurrentUserNameAsync();

            Groupes = await EmployeService.GetMaterielsGroupesAsync();
            Articles = Groupes.SelectMany(g => g.Articles).ToList();

            if (ArticleId > 0 && Articles.Any(a => a.ArticleId == ArticleId))
                SelectedArticleId = ArticleId;

            IsLoading = false;
        }

        // ── Sélection du type d'incident ───────────────────────
        private void SelectType(string type)
        {
            TypeIncident = type;
            StateHasChanged();
        }

        // ── Slider urgence ─────────────────────────────────────
        private void OnUrgencyChange(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int value))
            {
                Urgence = value;
                StateHasChanged();
            }
        }

        // ── Soumission ─────────────────────────────────────────
        private async Task SoumettreIncident()
        {
            ErrorMessage = string.Empty;

            // Validation sur SelectedArticleId (pas SelectedAffectationId)
            if (SelectedArticleId <= 0)
            {
                ErrorMessage = "Veuillez sélectionner un article.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                ErrorMessage = "Veuillez décrire le problème.";
                return;
            }

            try
            {
                IsSubmitting = true;

                var article = Articles.FirstOrDefault(a => a.ArticleId == SelectedArticleId);
                if (article == null) { ErrorMessage = "Article introuvable."; return; }

                var result = await IncidentService.SignalerIncidentAsync(new SignalerIncidentRequestDto
                {
                    AffectationId = article.AffectationId,  // ← tiré de l'article, pas d'une variable séparée
                    ArticleId = article.ArticleId,
                    TypeIncident = TypeIncident,
                    Urgence = Urgence,
                    Description = Description
                });

                if (result.Success)
                    Navigation.NavigateTo($"/achat/incident/success?numero={result.NumeroIncident}");
                else
                    ErrorMessage = result.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur : {ex.Message}";
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        // ── Helpers UI ─────────────────────────────────────────
        private string GetUrgencyLabel()
        {
            if (Urgence <= 33) return "FAIBLE";
            if (Urgence <= 66) return "MOYEN";
            return "CRITIQUE";
        }

        private string GetUrgencyClass()
        {
            if (Urgence <= 33) return "urgency-low";
            if (Urgence <= 66) return "urgency-medium";
            return "urgency-critical";
        }

        private string GetUserInitials()
        {
            var parts = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2)
                return parts[0][..2].ToUpper();
            return "??";
        }
        // Helper pour afficher la désignation dans le select :
        private string GetDesignation(int affectationId)
        {
            var groupe = Groupes.FirstOrDefault(g =>
                g.Articles.Any(a => a.AffectationId == affectationId));
            return groupe?.Designation ?? "Équipement";
        }
    }
}