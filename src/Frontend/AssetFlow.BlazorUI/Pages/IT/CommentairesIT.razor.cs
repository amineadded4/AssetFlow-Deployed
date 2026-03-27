// ============================================================
// AssetFlow.BlazorUI / Pages / IT / CommentairesIT.razor.cs
// MISE À JOUR : modal détail + suppression par IT
// ============================================================

using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class CommentairesIT : ComponentBase
    {
        [Inject] private EmployeService       EmployeService { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage   { get; set; } = default!;
        [Inject] private IJSRuntime           JS             { get; set; } = default!;

        // ── Données ──────────────────────────────────────────────
        private List<CommentaireITDto> Commentaires        { get; set; } = new();
        private List<CommentaireITDto> CommentairesFiltres { get; set; } = new();

        private bool   IsLoading    { get; set; } = true;
        private string ErrorMessage { get; set; } = string.Empty;

        // ── Filtres ───────────────────────────────────────────────
        private string SearchQuery { get; set; } = string.Empty;
        private string _roleFiltre = string.Empty;

        // ── Modal détail ──────────────────────────────────────────
        private bool              ModalOuvert            { get; set; } = false;
        private CommentaireITDto? CommentaireSelectionne { get; set; } = null;
        private string            ModalFeedback          { get; set; } = string.Empty;
        private bool              ModalFeedbackSucces    { get; set; } = false;

        // ── Suppression ───────────────────────────────────────────
        private int? SupprimerEnCours { get; set; } = null;

        // ── Sidebar / User ────────────────────────────────────────
        private bool   _menuOpen       = false;
        private string _nomUtilisateur = "Agent IT";
        private string _initiales      = "IT";
        private string      _roleUtilisateur = "Service IT";
        private bool _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        // ── Accordéon groupes matériel ────────────────────────────
        private HashSet<int> _groupesOuverts = new();

        // ── Init ──────────────────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            await ChargerInfosUtilisateur();
            await ChargerCommentaires();
        }

        private async Task ChargerInfosUtilisateur()
        {
            try
            {
                var nom = await JS.InvokeAsync<string?>("eval",
                    "localStorage.getItem('user_name') || localStorage.getItem('userFullName')");
                _roleUtilisateur = await LocalStorage.GetItemAsync<string>("user_role") ?? "IT";
                if (!string.IsNullOrWhiteSpace(nom))
                {
                    _nomUtilisateur = Nettoyer(nom);
                    var parts = _nomUtilisateur.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    _initiales = parts.Length >= 2
                        ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                        : _nomUtilisateur[..Math.Min(2, _nomUtilisateur.Length)].ToUpper();
                }
                
            }
            catch { }
        }

        // ── Chargement ────────────────────────────────────────────
        private async Task ChargerCommentaires()
        {
            try
            {
                IsLoading    = true;
                ErrorMessage = string.Empty;
                StateHasChanged();

                Commentaires = await EmployeService.GetTousLesCommentairesAsync(
                    string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery);

                AppliquerFiltres();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        // ── Filtres locaux ────────────────────────────────────────
        private void AppliquerFiltres()
        {
            var liste = Commentaires.AsEnumerable();
            if (!string.IsNullOrEmpty(_roleFiltre))
                liste = liste.Where(c => c.AuteurRole == _roleFiltre);
            CommentairesFiltres = liste.ToList();
        }

        // ── Accordéon ────────────────────────────────────────────
        private void ToggleGroupe(int materielId)
        {
            if (_groupesOuverts.Contains(materielId))
                _groupesOuverts.Remove(materielId);
            else
                _groupesOuverts.Add(materielId);
            StateHasChanged();
        }

        private void OnSearchInput(ChangeEventArgs e)
        {
            SearchQuery = e.Value?.ToString() ?? string.Empty;
            _ = ChargerCommentaires();
        }

        private void ClearSearch()
        {
            SearchQuery = string.Empty;
            _ = ChargerCommentaires();
        }

        private void SetRoleFiltre(string role)
        {
            _roleFiltre = role;
            AppliquerFiltres();
            StateHasChanged();
        }

        private void ResetFiltres()
        {
            SearchQuery = string.Empty;
            _roleFiltre = string.Empty;
            _ = ChargerCommentaires();
        }

        // ── Modal détail ──────────────────────────────────────────
        private void OuvrirModal(CommentaireITDto c)
        {
            CommentaireSelectionne = c;
            ModalFeedback          = string.Empty;
            ModalFeedbackSucces    = false;
            ModalOuvert            = true;
        }

        private void FermerModal()
        {
            ModalOuvert            = false;
            CommentaireSelectionne = null;
            ModalFeedback          = string.Empty;
        }

        // ── Suppression (IT peut supprimer tout commentaire) ──────
        private async Task SupprimerCommentaire(int commentaireId)
        {
            SupprimerEnCours = commentaireId;
            ModalFeedback    = string.Empty;
            StateHasChanged();

            // L'IT utilise un endpoint admin : on passe 0 comme userId
            // car côté backend, si userId == 0, on bypass la vérification d'auteur.
            // (Voir note ci-dessous sur le patch backend optionnel)
            var result = await EmployeService.SupprimerCommentaireITAsync(commentaireId);

            SupprimerEnCours = null;

            if (result.Succes)
            {
                // Suppression locale immédiate dans toutes les listes
                Commentaires.RemoveAll(c => c.Id == commentaireId);
                AppliquerFiltres();

                // Si le modal est ouvert sur ce commentaire, feedback puis fermeture
                if (ModalOuvert && CommentaireSelectionne?.Id == commentaireId)
                {
                    ModalFeedback       = "Commentaire supprimé avec succès.";
                    ModalFeedbackSucces = true;
                    StateHasChanged();
                    await Task.Delay(1200);
                    FermerModal();
                }
            }
            else
            {
                if (ModalOuvert && CommentaireSelectionne?.Id == commentaireId)
                {
                    ModalFeedback       = result.Message;
                    ModalFeedbackSucces = false;
                }
            }

            StateHasChanged();
        }

        // ── Helpers UI ────────────────────────────────────────────
        private static string GetRoleClass(string role) => role switch
        {
            "IT"          => "it",
            "EquipeAchat" => "achat",
            _             => "employe"
        };

        private static string GetRoleLabel(string role) => role switch
        {
            "IT"          => "IT",
            "EquipeAchat" => "Achat",
            "Employe"     => "Employé",
            _             => role
        };

        private static string Nettoyer(string v)
        {
            v = v.Trim();
            if (v.Length >= 2 &&
                ((v.StartsWith('"') && v.EndsWith('"')) ||
                 (v.StartsWith('\'') && v.EndsWith('\''))))
                v = v[1..^1].Trim();
            return v;
        }
    }
}
