using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using AssetFlow.BlazorUI.DTOs;

namespace AssetFlow.BlazorUI.Pages.Employe
{
    public partial class SignalerIncident
    {
        [Parameter] public int AffectationId { get; set; } = 0;
        [Parameter] public int ArticleId     { get; set; } = 0;

        private List<ArticleAffecteDto>        Articles { get; set; } = new();
        private List<MaterielAffecteGroupeDto> Groupes  { get; set; } = new();
        private int SelectedArticleId { get; set; } = 0;

        [Inject] private IncidentService   IncidentService { get; set; } = default!;
        [Inject] private EmployeService    EmployeService  { get; set; } = default!;
        [Inject] private NavigationManager Navigation      { get; set; } = default!;
        [Inject] private VoiceCommandService VoiceSvc { get; set; } = default!;

        private string TypeIncident { get; set; } = "Panne";
        private string Description  { get; set; } = string.Empty;
        private int    Urgence      { get; set; } = 50;

        private bool   IsLoading    { get; set; } = true;
        private bool   IsSubmitting { get; set; } = false;
        private string ErrorMessage { get; set; } = string.Empty;
        private bool _menuOpen = false;

        protected override async Task OnInitializedAsync()
        {
            VoiceSvc.OnCommand += HandleVoiceCommand;
            Groupes  = await EmployeService.GetMaterielsGroupesAsync();
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
                                $"/employe/equipement/{AffectationId}/article/{ArticleId}");
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

        private void SelectType(string type) { TypeIncident = type; StateHasChanged(); }

        private void OnUrgencyChange(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int value)) { Urgence = value; StateHasChanged(); }
        }

        private async Task SoumettreIncident()
        {
            ErrorMessage = string.Empty;

            if (SelectedArticleId <= 0) { ErrorMessage = "Veuillez sélectionner un article."; return; }
            if (string.IsNullOrWhiteSpace(Description)) { ErrorMessage = "Veuillez décrire le problème."; return; }

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
                    Navigation.NavigateTo($"/employe/incident/success?numero={result.NumeroIncident}");
                else
                    ErrorMessage = result.Message;
            }
            catch (Exception ex) { ErrorMessage = $"Erreur : {ex.Message}"; }
            finally               { IsSubmitting = false; }
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

        private string GetDesignation(int affectationId)
        {
            var groupe = Groupes.FirstOrDefault(g => g.Articles.Any(a => a.AffectationId == affectationId));
            return groupe?.Designation ?? "Équipement";
        }
    }
}