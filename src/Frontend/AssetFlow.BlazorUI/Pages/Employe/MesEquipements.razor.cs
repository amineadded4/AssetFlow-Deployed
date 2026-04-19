using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using AssetFlow.BlazorUI.DTOs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Pages.Employe
{
    public partial class MesEquipements
    {
        [Inject] private EmployeService    EmployeService { get; set; } = default!;
        [Inject] private NavigationManager Navigation     { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        // ── Données principales ────────────────────────────────
        private List<MaterielAffecteGroupeDto> MaterielsGroupes        { get; set; } = new();
        private List<MaterielAffecteGroupeDto> MaterielsGroupesFiltres { get; set; } = new();


        private bool   IsLoading    { get; set; } = true;
        private string ErrorMessage { get; set; } = string.Empty;

        // ── Recherche principale ───────────────────────────────
        private string SearchQuery { get; set; } = string.Empty;

        // ── Modal articles ─────────────────────────────────────
        private bool                      ModalOuvert         { get; set; } = false;
        private MaterielAffecteGroupeDto? MaterielSelectionne { get; set; } = null;
        private string                    ModalSearchQuery    { get; set; } = string.Empty;

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
        private string UserRole { get; set; } = "Employé";
        private bool _menuOpen = false;
        private HubConnection? _hubConnection;

        // ── Initialisation ─────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            await LoadMaterielsGroupesAsync();
            await ConnecterSignalR();
        }
        private async Task ConnecterSignalR()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5235/dashboardhub", options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        try
                        {
                            return await JS.InvokeAsync<string?>("eval",
                                "localStorage.getItem('access_token') || localStorage.getItem('token')");
                        }
                        catch { return null; }
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            // Toute action (affectation, révocation, incident...) → recharger
            _hubConnection.On("DashboardUpdated", async () =>
            {
                await InvokeAsync(async () =>
                {
                    // Ne pas mettre IsLoading=true pour éviter le flash
                    try
                    {
                        MaterielsGroupes = await EmployeService.GetMaterielsGroupesAsync();
                        FiltrerMateriels(); // ← réappliquer le filtre actif au lieu de juste =MaterielsGroupes
                    }
                    catch { /* silencieux */ }
                    finally
                    {
                        IsLoading = false;
                        StateHasChanged();
                    }
                });
            });

            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinDashboard");
            }
            catch { /* reste statique si SignalR non dispo */ }
        }
        private async void AfficherToastVoice(string msg)
        {
            ErrorMessage = msg;
            StateHasChanged();
            await Task.Delay(3000);
            ErrorMessage = string.Empty;
            StateHasChanged();
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                try { await _hubConnection.InvokeAsync("LeaveDashboard"); } catch { }
                await _hubConnection.DisposeAsync();
            }
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


        // ── Navigation ─────────────────────────────────────────
        private void NaviguerVersDetail(int affectationId, int articleId)
        {
            Navigation.NavigateTo($"/employe/equipement/{affectationId}/article/{articleId}");
        }
    }
}