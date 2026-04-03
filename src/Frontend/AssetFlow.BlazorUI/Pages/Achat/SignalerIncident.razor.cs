using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using AssetFlow.BlazorUI.DTOs;

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
        [Inject] private AssetFlow.BlazorUI.Services.VoiceCommandService VoiceSvc { get; set; } = default!;

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
        private bool        _sidebarOpen     = false;

        private void ToggleSidebar() => _sidebarOpen  = !_sidebarOpen;
        private string      _roleUtilisateur = "Service Achat";
        private bool _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        private bool _roleCharge = false; 

        // ── Initialisation ─────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            VoiceSvc.OnCommand += HandleVoiceCommand;
            UserName = await EmployeService.GetCurrentUserNameAsync();
            _roleUtilisateur = await EmployeService.GetCurrentUserRoleAsync(); // ← lire le bon champ !
            _roleCharge = true;

            Groupes = await EmployeService.GetMaterielsGroupesAsync();
            Articles = Groupes.SelectMany(g => g.Articles).ToList();

            if (ArticleId > 0 && Articles.Any(a => a.ArticleId == ArticleId))
                SelectedArticleId = ArticleId;

            IsLoading = false;
        }
         private Task HandleVoiceCommand(VoiceCommand cmd)
        {
            return InvokeAsync(async () =>
            {
                switch (cmd.Type)
                {
                    // ✅ Même logique que DetailsEquipement pour "incident"
                    case VoiceCommandType.VoirArticles:
                    case VoiceCommandType.VoirArticlesEquipement:
                    case VoiceCommandType.Navigation
                        when cmd.NavigateTo?.Contains("equipement") == true
                        || cmd.NavigateTo?.Contains("materiel") == true:
                        await Task.Delay(50);
                        if (AffectationId > 0)
                            Navigation.NavigateTo(
                                $"/achat/equipement/{AffectationId}/article/{ArticleId}");
                        break;
                    case VoiceCommandType.SignalerIncident:
                    case VoiceCommandType.SoumettreIncident:
                        await SoumettreIncident();
                        break;

                    case VoiceCommandType.Navigation when cmd.NavigateTo != null:
                        await Task.Delay(50);
                        Navigation.NavigateTo(cmd.NavigateTo);
                        break;
                }
                StateHasChanged();
            });
        }
        public ValueTask DisposeAsync()
        {
            VoiceSvc.OnCommand -= HandleVoiceCommand;
            return ValueTask.CompletedTask;
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

            // Validation sur SelectedArticleId
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
        private string GetDesignation(int affectationId)
        {
            var groupe = Groupes.FirstOrDefault(g =>
                g.Articles.Any(a => a.AffectationId == affectationId));
            return groupe?.Designation ?? "Équipement";
        }
    }
}