using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Blazored.LocalStorage;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class MesEquipements
    {
        [Inject] private EmployeService    EmployeService { get; set; } = default!;
        [Inject] private NavigationManager Navigation     { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage   { get; set; } = default!;
        [Inject] private VoiceCommandService VoiceSvc { get; set; } = default!;

        // ── Données ────────────────────────────────────────────
        private List<MaterielAffecteGroupeDto> MaterielsGroupes        { get; set; } = new();
        private List<MaterielAffecteGroupeDto> MaterielsGroupesFiltres { get; set; } = new();

        private bool   IsLoading     { get; set; } = true;
        private string ErrorMessage  { get; set; } = string.Empty;

        // ── Recherche principale ───────────────────────────────
        private string SearchQuery { get; set; } = string.Empty;

        // ── Modal articles ─────────────────────────────────────
        private bool                      ModalOuvert         { get; set; } = false;
        private MaterielAffecteGroupeDto? MaterielSelectionne  { get; set; } = null;
        private string                    ModalSearchQuery     { get; set; } = string.Empty;

        // ── Modal commentaire ──────────────────────────────────
        private bool                      ModalCommentaireOuvert { get; set; } = false;
        private MaterielAffecteGroupeDto? MaterielCommentaire    { get; set; } = null;
        private string                    CommentaireContenu     { get; set; } = string.Empty;
        private string                    CommentaireFeedback    { get; set; } = string.Empty;
        private bool                      CommentaireSucces      { get; set; } = false;
        private bool                      CommentaireEnvoi       { get; set; } = false;
        private bool                      CommentaireChargement  { get; set; } = false;
        private List<CommentaireDto>      CommentairesExistants  { get; set; } = new();
        private int?                      CommentaireSupprimerId  { get; set; } = null;

        // ── Info utilisateur ───────────────────────────────────
        private string UserName { get; set; } = "Utilisateur";
        private string UserRole { get; set; } = "Achat";
        private bool   _sidebarOpen = false;
        private bool _estAdmin => UserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        private void ToggleSidebar() => _sidebarOpen = !_sidebarOpen;

        // ── Init ───────────────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            VoiceSvc.OnCommand += HandleVoiceCommand;
            UserName = await LocalStorage.GetItemAsync<string>("user_name") ?? "Utilisateur";
            UserRole = await LocalStorage.GetItemAsync<string>("user_role") ?? "Achat";

            await LoadMaterielsGroupesAsync();
        }
        private Task HandleVoiceCommand(VoiceCommand cmd)
        {
            return InvokeAsync(async () =>
            {
                switch (cmd.Type)
                {
                    case VoiceCommandType.VoirArticlesEquipement:
                    {
                        var m = TrouverMateriel(cmd.Reference, cmd.Designation);
                        if (m != null) OuvrirModal(m);
                        else AfficherToastVoice($"Équipement introuvable.");
                        break;
                    }

                    case VoiceCommandType.VoirCommentairesEquipement:
                    {
                        var m = TrouverMateriel(cmd.Reference, cmd.Designation);
                        if (m != null) await OuvrirModalCommentaire(m);
                        else AfficherToastVoice($"Équipement introuvable.");
                        break;
                    }
                }
                StateHasChanged();
            });
        }
        // Recherche par référence OU par nom approximatif
        private MaterielAffecteGroupeDto? TrouverMateriel(string? reference, string? designation)
        {
            // Priorité à la référence exacte (SN-900)
            if (!string.IsNullOrWhiteSpace(reference))
            {
                var byRef = MaterielsGroupes.FirstOrDefault(m =>
                    m.Reference.Equals(reference, StringComparison.OrdinalIgnoreCase));
                if (byRef != null) return byRef;
            }

            // Sinon recherche par nom approché
            if (!string.IsNullOrWhiteSpace(designation))
            {
                var terms = designation.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return MaterielsGroupes
                    .Select(m => new
                    {
                        Materiel = m,
                        Score    = terms.Count(t => m.Designation.ToLower().Contains(t))
                    })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .Select(x => x.Materiel)
                    .FirstOrDefault();
            }

            return null;
        }
        private async void AfficherToastVoice(string msg)
        {
            ErrorMessage = msg;
            StateHasChanged();
            await Task.Delay(3000);
            ErrorMessage = string.Empty;
            StateHasChanged();
        }

        public ValueTask DisposeAsync()
        {
            VoiceSvc.OnCommand -= HandleVoiceCommand;
            return ValueTask.CompletedTask;
        }
        private async Task LoadMaterielsGroupesAsync()
        {
            try
            {
                IsLoading    = true;
                ErrorMessage = string.Empty;

                MaterielsGroupes        = await EmployeService.GetMaterielsGroupesAsync();
                MaterielsGroupesFiltres = MaterielsGroupes;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ── Recherche ──────────────────────────────────────────
        private void OnSearchInput(ChangeEventArgs e)
        {
            SearchQuery = e.Value?.ToString() ?? string.Empty;
            FiltrerMateriels();
        }

        private void FiltrerMateriels()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                MaterielsGroupesFiltres = MaterielsGroupes;
                return;
            }

            var q = SearchQuery.Trim().ToLower();
            MaterielsGroupesFiltres = MaterielsGroupes
                .Where(m =>
                    m.Designation.ToLower().Contains(q) ||
                    m.Reference.ToLower().Contains(q)   ||
                    m.Categorie.ToLower().Contains(q))
                .ToList();
        }

        // ── Modal articles ─────────────────────────────────────
        private void OuvrirModal(MaterielAffecteGroupeDto materiel)
        {
            MaterielSelectionne = materiel;
            ModalSearchQuery    = string.Empty;
            ModalOuvert         = true;
        }

        private void FermerModal()
        {
            ModalOuvert         = false;
            MaterielSelectionne = null;
            ModalSearchQuery    = string.Empty;
        }

        private void OnModalSearchInput(ChangeEventArgs e)
        {
            ModalSearchQuery = e.Value?.ToString() ?? string.Empty;
            StateHasChanged();
        }

        // ── Ouvre le modal commentaire ────────────────────────────────
private async Task OuvrirModalCommentaire(MaterielAffecteGroupeDto materiel)
{
    MaterielCommentaire    = materiel;
    CommentaireContenu     = string.Empty;
    CommentaireFeedback    = string.Empty;
    CommentaireSucces      = false;
    CommentaireEnvoi       = false;
    CommentaireChargement  = true;
    CommentaireSupprimerId = null;
    ModalCommentaireOuvert = true;
    StateHasChanged();

    CommentairesExistants = await EmployeService.GetCommentairesMaterielAsync(materiel.MaterielId);
    CommentaireChargement = false;
    StateHasChanged();
}

// ── Ferme le modal commentaire ────────────────────────────────
private void FermerModalCommentaire()
{
    ModalCommentaireOuvert = false;
    MaterielCommentaire    = null;
    CommentaireContenu     = string.Empty;
    CommentaireFeedback    = string.Empty;
    CommentairesExistants  = new();
    CommentaireSupprimerId = null;
}

// ── Envoie un commentaire ─────────────────────────────────────
private async Task EnvoyerCommentaire()
{
    if (MaterielCommentaire == null || string.IsNullOrWhiteSpace(CommentaireContenu))
        return;

    CommentaireEnvoi    = true;
    CommentaireFeedback = string.Empty;
    StateHasChanged();

    var result = await EmployeService.AjouterCommentaireAsync(
        MaterielCommentaire.MaterielId, CommentaireContenu);

    CommentaireEnvoi  = false;
    CommentaireSucces = result.Succes;

    if (result.Succes)
    {
        CommentaireFeedback = "Votre commentaire a bien été enregistré !";
        CommentaireContenu  = string.Empty;

        MaterielCommentaire.NombreCommentaires++;

        CommentairesExistants = await EmployeService.GetCommentairesMaterielAsync(
            MaterielCommentaire.MaterielId);
    }
    else
    {
        CommentaireFeedback = result.Message;
    }

    StateHasChanged();
}

// ── Supprime un commentaire ───────────────────────────────────
private async Task SupprimerCommentaire(int commentaireId)
{
    CommentaireSupprimerId = commentaireId;   // active le spinner sur le bon bouton
    CommentaireFeedback    = string.Empty;
    StateHasChanged();

    var result = await EmployeService.SupprimerCommentaireAsync(commentaireId);

    CommentaireSupprimerId = null;
    CommentaireSucces      = result.Succes;

    if (result.Succes)
    {
        CommentaireFeedback = "Commentaire supprimé.";

        // Mise à jour locale immédiate (sans rechargement complet)
        CommentairesExistants.RemoveAll(c => c.Id == commentaireId);

        if (MaterielCommentaire != null)
        {
            MaterielCommentaire.NombreCommentaires =
                Math.Max(0, MaterielCommentaire.NombreCommentaires - 1);

            var carte = MaterielsGroupes.FirstOrDefault(
                m => m.MaterielId == MaterielCommentaire.MaterielId);
            if (carte != null)
                carte.NombreCommentaires = MaterielCommentaire.NombreCommentaires;
        }
    }
    else
    {
        CommentaireFeedback = result.Message;
    }

    StateHasChanged();
}

        // ── Navigation vers détail ─────────────────────────────
        private void NaviguerVersDetail(int affectationId, int articleId)
        {
            Navigation.NavigateTo($"/achat/equipement/{affectationId}/article/{articleId}");
        }
        private string GetStatutLabel(string statut) => statut switch
        {
            "EnCours"   => "BON",
            "Retourne"  => "RETOURNÉ",
            "Perdu"     => "PERDU",
            "Endommage" => "ENDOMMAGÉ",
            _           => statut.ToUpper()
        };

        private string GetInitials()
        {
            var parts = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
            return "??";
        }
    }
}