// ============================================================
// AssetFlow.BlazorUI / Pages / Employe / MesEquipements.razor.cs
// FICHIER COMPLET
// ============================================================

using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Pages.Employe
{
    public partial class MesEquipements
    {
        [Inject] private EmployeService    EmployeService { get; set; } = default!;
        [Inject] private NavigationManager Navigation     { get; set; } = default!;

        // ── Données principales ────────────────────────────────
        private List<MaterielAffecteGroupeDto> MaterielsGroupes        { get; set; } = new();
        private List<MaterielAffecteGroupeDto> MaterielsGroupesFiltres { get; set; } = new();

        private bool   IsLoading    { get; set; } = true;
        private string ErrorMessage { get; set; } = string.Empty;

        // ── Recherche principale ───────────────────────────────
        private string SearchQuery { get; set; } = string.Empty;

        // ── Modal articles (existant) ──────────────────────────
        private bool                      ModalOuvert        { get; set; } = false;
        private MaterielAffecteGroupeDto? MaterielSelectionne { get; set; } = null;
        private string                    ModalSearchQuery    { get; set; } = string.Empty;

        // ── Modal commentaire (NOUVEAU) ────────────────────────
        private bool                      ModalCommentaireOuvert { get; set; } = false;
        private MaterielAffecteGroupeDto? MaterielCommentaire    { get; set; } = null;
        private string                    CommentaireContenu     { get; set; } = string.Empty;
        private string                    CommentaireFeedback    { get; set; } = string.Empty;
        private bool                      CommentaireSucces      { get; set; } = false;
        private bool                      CommentaireEnvoi       { get; set; } = false;
        private bool                      CommentaireChargement  { get; set; } = false;
        private List<CommentaireDto>      CommentairesExistants  { get; set; } = new();

        // ── Info utilisateur ───────────────────────────────────
        private string UserName { get; set; } = "Utilisateur";
        private string UserRole { get; set; } = "Employé";

        // ── Initialisation ─────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            await LoadUserInfoAsync();
            await LoadMaterielsGroupesAsync();
        }

        private async Task LoadUserInfoAsync()
        {
            UserName = await EmployeService.GetCurrentUserNameAsync();
            UserRole = await EmployeService.GetCurrentUserRoleAsync();
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

        // ── Modal articles (existant) ──────────────────────────
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

        // ── Modal commentaire (NOUVEAU) ────────────────────────

        private async Task OuvrirModalCommentaire(MaterielAffecteGroupeDto materiel)
        {
            MaterielCommentaire    = materiel;
            CommentaireContenu     = string.Empty;
            CommentaireFeedback    = string.Empty;
            CommentaireSucces      = false;
            CommentaireEnvoi       = false;
            CommentaireChargement  = true;
            ModalCommentaireOuvert = true;
            StateHasChanged();

            // Charger les commentaires existants sur ce matériel
            CommentairesExistants = await EmployeService.GetCommentairesMaterielAsync(materiel.MaterielId);
            CommentaireChargement = false;
            StateHasChanged();
        }

        private void FermerModalCommentaire()
        {
            ModalCommentaireOuvert = false;
            MaterielCommentaire    = null;
            CommentaireContenu     = string.Empty;
            CommentaireFeedback    = string.Empty;
            CommentairesExistants  = new();
        }

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

                // Mettre à jour le compteur sur la carte sans recharger
                MaterielCommentaire.NombreCommentaires++;
                var carte = MaterielsGroupes.FirstOrDefault(
                    m => m.MaterielId == MaterielCommentaire.MaterielId);
                if (carte != null) carte.NombreCommentaires++;

                // Rafraîchir la liste des commentaires dans le modal
                CommentairesExistants = await EmployeService.GetCommentairesMaterielAsync(
                    MaterielCommentaire.MaterielId);
            }
            else
            {
                CommentaireFeedback = result.Message;
            }

            StateHasChanged();
        }

        // ── Navigation vers détail article ────────────────────
        private void NaviguerVersDetail(int affectationId, int articleId)
        {
            Navigation.NavigateTo($"/employe/equipement/{affectationId}/article/{articleId}");
        }

        // ── Helpers UI ─────────────────────────────────────────
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
