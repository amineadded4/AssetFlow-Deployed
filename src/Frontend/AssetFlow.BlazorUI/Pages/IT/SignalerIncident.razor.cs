using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class SignalerIncident
    {
        [Parameter] public int AffectationId { get; set; } = 0;
        [Parameter] public int ArticleId     { get; set; } = 0;

        [Inject] private IncidentService      IncidentService { get; set; } = default!;
        [Inject] private EmployeService       EmployeService  { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage    { get; set; } = default!;
        [Inject] private NavigationManager    Navigation      { get; set; } = default!;

        private List<ArticleAffecteDto>        Articles { get; set; } = new();
        private List<MaterielAffecteGroupeDto> Groupes  { get; set; } = new();
        private int    SelectedArticleId { get; set; } = 0;

        private string TypeIncident   { get; set; } = "Panne";
        private string Description    { get; set; } = string.Empty;
        private int    Urgence        { get; set; } = 50;
        private bool   IsLoading      { get; set; } = true;
        private bool   IsSubmitting   { get; set; } = false;
        private string ErrorMessage   { get; set; } = string.Empty;
        private string UserName       { get; set; } = "Utilisateur";
        private string      _roleUtilisateur = "Service IT";
        private bool _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        private bool   _menuOpen      = false;

        protected override async Task OnInitializedAsync()
        {
            UserName = await LocalStorage.GetItemAsync<string>("user_name") ?? "IT";
            _roleUtilisateur = await LocalStorage.GetItemAsync<string>("user_role") ?? "IT";

            Groupes  = await EmployeService.GetMaterielsGroupesAsync();
            Articles = Groupes.SelectMany(g => g.Articles).ToList();

            if (ArticleId > 0 && Articles.Any(a => a.ArticleId == ArticleId))
                SelectedArticleId = ArticleId;

            IsLoading = false;
        }

        private void SelectType(string type)
        {
            TypeIncident = type;
            StateHasChanged();
        }

        private void OnUrgencyChange(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int value))
            {
                Urgence = value;
                StateHasChanged();
            }
        }

        private async Task SoumettreIncident()
        {
            ErrorMessage = string.Empty;

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
                    AffectationId = article.AffectationId,
                    ArticleId     = article.ArticleId,
                    TypeIncident  = TypeIncident,
                    Urgence       = Urgence,
                    Description   = Description
                });

                if (result.Success)
                    Navigation.NavigateTo($"/it/incident/success?numero={result.NumeroIncident}");
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

        private string GetDesignation(int affectationId)
        {
            var groupe = Groupes.FirstOrDefault(g => g.Articles.Any(a => a.AffectationId == affectationId));
            return groupe?.Designation ?? "Équipement";
        }

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
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
            return "IT";
        }
    }
}