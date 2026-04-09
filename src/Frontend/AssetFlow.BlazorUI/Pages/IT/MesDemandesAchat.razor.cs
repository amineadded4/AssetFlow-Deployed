using AssetFlow.BlazorUI.DTOs;
using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class MesDemandesAchat
    {
        [Inject] private DemandeAchatITClientService DemandeService { get; set; } = default!;
        [Inject] private NavigationManager           Navigation      { get; set; } = default!;
        [Inject] private ILocalStorageService        LocalStorage    { get; set; } = default!;

        private bool   IsLoading      { get; set; } = true;
        private bool   _menuOpen      = false;
        private string UserName       { get; set; } = "IT";
        private string ErrorMessage   { get; set; } = string.Empty;
        private string SuccessMessage { get; set; } = string.Empty;
        private int? _userId = null;

        private List<DemandeAchatITDto> Demandes         { get; set; } = new();
        private List<DemandeAchatITDto> DemandesFiltrees { get; set; } = new();

        private IEnumerable<DemandeAchatITDto> PagedDemandes =>
            DemandesFiltrees.Skip((CurrentPage - 1) * PageSize).Take(PageSize);

        private string? FilterStatut      { get; set; } = null;
        private string  SearchQuery       { get; set; } = string.Empty;
        private string  SortOrder         { get; set; } = "date_desc";
        private int?    SelectedDemandeId { get; set; } = null;

        private int CurrentPage { get; set; } = 1;
        private const int PageSize = 10;
        private int TotalPages => Math.Max(1, (int)Math.Ceiling(DemandesFiltrees.Count / (double)PageSize));

        // ── Panneau création ─────────────────────────────────────
        private bool   _showCreatePanel = false;
        private bool   _isSaving        = false;
        private CreateDemandeForm _form  = new();
        private string _formError        = string.Empty;

        // ── Panneau édition ──────────────────────────────────────
        private bool              _showEditPanel = false;
        private bool              _isUpdating    = false;
        private int               _editDemandeId = 0;
        private CreateDemandeForm _editForm      = new();
        private string            _editFormError = string.Empty;

        // ── Modal suppression ────────────────────────────────────
        private bool               _showDeleteModal   = false;
        private bool               _isDeleting        = false;
        private DemandeAchatITDto? _demandeASupprimer = null;
        private string      _roleUtilisateur = "Service IT";
        private bool _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        // ── Init ─────────────────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            UserName         = await LocalStorage.GetItemAsync<string>("user_name") ?? "IT";
            _roleUtilisateur = await LocalStorage.GetItemAsync<string>("user_role") ?? "IT";

            var userIdRaw = await LocalStorage.GetItemAsync<string>("user_id");
            _userId = int.TryParse(userIdRaw, out var parsedId) ? parsedId : null;

            await LoadDemandesAsync();
        }

        private async Task LoadDemandesAsync()
        {
            IsLoading = true;
            StateHasChanged();
            try
            {
                Demandes = await DemandeService.GetDemandesAsync(_userId);
                AppliquerFiltres();
            }
            catch
            {
                ErrorMessage = "Impossible de charger les demandes. Veuillez réessayer.";
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private void AppliquerFiltres()
        {
            var result = Demandes.AsEnumerable();

            if (!string.IsNullOrEmpty(FilterStatut))
                result = result.Where(d => d.Statut == FilterStatut);

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.Trim().ToLower();
                result = result.Where(d =>
                    d.NomProduit.ToLower().Contains(q)              ||
                    d.Reference.ToLower().Contains(q)               ||
                    (d.Description?.ToLower().Contains(q) ?? false) ||
                    d.Lignes.Any(l => l.NomProduit.ToLower().Contains(q) ||
                                     l.Reference.ToLower().Contains(q)));
            }

            result = SortOrder switch
            {
                "date_asc"      => result.OrderBy(d => d.DateCreation),
                "produit_asc"   => result.OrderBy(d => d.NomProduit),
                "quantite_desc" => result.OrderByDescending(d => d.Quantite),
                _               => result.OrderByDescending(d => d.DateCreation)
            };

            DemandesFiltrees  = result.ToList();
            CurrentPage       = 1;
            SelectedDemandeId = null;
        }

        private void SetFilter(string? statut) { FilterStatut = statut; AppliquerFiltres(); }

        private void OnSearchInput(ChangeEventArgs e)
        {
            SearchQuery = e.Value?.ToString() ?? string.Empty;
            AppliquerFiltres();
        }

        private void ClearSearch() { SearchQuery = string.Empty; AppliquerFiltres(); }

        private void OnSortChange(ChangeEventArgs e)
        {
            SortOrder = e.Value?.ToString() ?? "date_desc";
            AppliquerFiltres();
        }

        private void ToggleExpand(int id)
            => SelectedDemandeId = SelectedDemandeId == id ? null : id;

        private void NavigateToOffres(int demandeId)
            => Navigation.NavigateTo($"/it/offres/{demandeId}");

        // ════════════════════════════════════════
        // PANNEAU CRÉATION
        // ════════════════════════════════════════
        private void OpenCreatePanel()
        {
            _form            = new CreateDemandeForm();
            _formError       = string.Empty;
            ErrorMessage     = string.Empty;
            SuccessMessage   = string.Empty;
            _showCreatePanel = true;
            _showEditPanel   = false;
        }

        private void CloseCreatePanel() => _showCreatePanel = false;

        private void AjouterLigne() => _form.Lignes.Add(new LigneForm());
        private void SupprimerLigne(LigneForm ligne)
        {
            if (_form.Lignes.Count > 1)
                _form.Lignes.Remove(ligne);
        }

        private async Task SubmitCreate()
        {
            _formError = string.Empty;

            if (string.IsNullOrWhiteSpace(_form.NomDemande))
            {
                _formError = "Le titre de la demande est obligatoire.";
                return;
            }

            foreach (var ligne in _form.Lignes)
            {
                ligne.Erreur = string.Empty;
                if (string.IsNullOrWhiteSpace(ligne.NomProduit))
                    ligne.Erreur = "Le nom du produit est obligatoire.";
                else if (ligne.Quantite < 1)
                    ligne.Erreur = "La quantité doit être au moins 1.";
            }

            if (_form.Lignes.Any(l => !string.IsNullOrEmpty(l.Erreur))) return;

            _isSaving = true;
            StateHasChanged();

            try
            {
                await DemandeService.CreateDemandeAsync(new CreateDemandeAchatDto
                {
                    Utilisateur = UserName,
                    UserId       = _userId,          // ← envoyé depuis le localStorage
                    NomProduit   = _form.NomDemande.Trim(),
                    Description  = _form.Description?.Trim(),
                    DemandeurNom = UserName,
                    Lignes       = _form.Lignes.Select(l => new CreateLigneDemandeDto
                    {
                        Reference   = l.Reference?.Trim() ?? string.Empty,
                        NomProduit  = l.NomProduit.Trim(),
                        Quantite    = l.Quantite,
                        Description = l.Description?.Trim()
                    }).ToList()
                });

                SuccessMessage   = "Demande soumise avec succès !";
                _showCreatePanel = false;
                await LoadDemandesAsync();
            }
            catch
            {
                ErrorMessage = "Erreur lors de la soumission. Veuillez réessayer.";
            }
            finally
            {
                _isSaving = false;
                StateHasChanged();
            }
        }

        // ════════════════════════════════════════
        // PANNEAU ÉDITION
        // ════════════════════════════════════════
        private void OpenEditPanel(DemandeAchatITDto demande)
        {
            _editDemandeId = demande.IdDemande;
            _editFormError = string.Empty;
            ErrorMessage   = string.Empty;
            SuccessMessage = string.Empty;

            _editForm = new CreateDemandeForm
            {
                NomDemande  = demande.NomProduit,
                Description = demande.Description,
                Lignes      = demande.Lignes.Any()
                    ? demande.Lignes.Select(l => new LigneForm
                    {
                        Reference   = l.Reference ?? string.Empty,
                        NomProduit  = l.NomProduit,
                        Quantite    = l.Quantite,
                        Description = l.Description
                    }).ToList()
                    : new List<LigneForm> { new LigneForm() }
            };

            _showEditPanel   = true;
            _showCreatePanel = false;
        }

        private void CloseEditPanel() => _showEditPanel = false;

        private void AjouterLigneEdit()     => _editForm.Lignes.Add(new LigneForm());
        private void SupprimerLigneEdit(LigneForm ligne)
        {
            if (_editForm.Lignes.Count > 1)
                _editForm.Lignes.Remove(ligne);
        }

        private async Task SubmitEdit()
        {
            _editFormError = string.Empty;

            if (string.IsNullOrWhiteSpace(_editForm.NomDemande))
            {
                _editFormError = "Le titre de la demande est obligatoire.";
                return;
            }

            foreach (var ligne in _editForm.Lignes)
            {
                ligne.Erreur = string.Empty;
                if (string.IsNullOrWhiteSpace(ligne.NomProduit))
                    ligne.Erreur = "Le nom du produit est obligatoire.";
                else if (ligne.Quantite < 1)
                    ligne.Erreur = "La quantité doit être au moins 1.";
            }

            if (_editForm.Lignes.Any(l => !string.IsNullOrEmpty(l.Erreur))) return;

            _isUpdating = true;
            StateHasChanged();

            try
            {
                await DemandeService.UpdateDemandeAsync(_editDemandeId, new UpdateDemandeAchatDto
                {
                    Utilisateur = UserName,
                    NomProduit  = _editForm.NomDemande.Trim(),
                    Description = _editForm.Description?.Trim(),
                    Lignes      = _editForm.Lignes.Select(l => new CreateLigneDemandeDto
                    {
                        Reference   = l.Reference?.Trim() ?? string.Empty,
                        NomProduit  = l.NomProduit.Trim(),
                        Quantite    = l.Quantite,
                        Description = l.Description?.Trim()
                    }).ToList()
                });

                SuccessMessage = "Demande modifiée avec succès !";
                _showEditPanel = false;
                await LoadDemandesAsync();
            }
            catch
            {
                ErrorMessage = "Erreur lors de la modification. Veuillez réessayer.";
            }
            finally
            {
                _isUpdating = false;
                StateHasChanged();
            }
        }

        // ════════════════════════════════════════
        // MODAL SUPPRESSION
        // ════════════════════════════════════════
        private void OuvrirConfirmSuppression(DemandeAchatITDto demande)
        {
            _demandeASupprimer = demande;
            _showDeleteModal   = true;
            ErrorMessage       = string.Empty;
            SuccessMessage     = string.Empty;
        }

        private void AnnulerSuppression()
        {
            _showDeleteModal   = false;
            _demandeASupprimer = null;
        }

        private async Task ConfirmerSuppression()
        {
            if (_demandeASupprimer == null) return;

            _isDeleting = true;
            StateHasChanged();

            try
            {
                await DemandeService.DeleteDemandeAsync(_demandeASupprimer.IdDemande);
                SuccessMessage     = $"Demande \"{_demandeASupprimer.NomProduit}\" supprimée avec succès.";
                _showDeleteModal   = false;
                _demandeASupprimer = null;
                await LoadDemandesAsync();
            }
            catch
            {
                ErrorMessage = "Erreur lors de la suppression. Veuillez réessayer.";
            }
            finally
            {
                _isDeleting = false;
                StateHasChanged();
            }
        }

        // ── Pagination ───────────────────────────────────────────
        private void PrevPage()         { if (CurrentPage > 1)         CurrentPage--; }
        private void NextPage()         { if (CurrentPage < TotalPages) CurrentPage++; }
        private void GoToPage(int page) => CurrentPage = page;

        private string GetInitials()
        {
            var parts = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
            return "IT";
        }

        private static string GetStatutLabel(string statut) => statut switch
        {
            "en_attente"          => "En attente",
            "en_cours_traitement" => "En cours",
            "commande"            => "Commandé",
            "traite"              => "Traité",
            "approuve"            => "Approuvé",
            "refuse"              => "Refusé",
            _                     => statut
        };

        private static string GetRelativeDate(DateTime date)
        {
            var diff = DateTime.Now - date;
            if (diff.TotalMinutes < 60)  return $"il y a {(int)diff.TotalMinutes} min";
            if (diff.TotalHours   < 24)  return $"il y a {(int)diff.TotalHours} h";
            if (diff.TotalDays    < 2)   return "hier";
            if (diff.TotalDays    < 7)   return $"il y a {(int)diff.TotalDays} jours";
            if (diff.TotalDays    < 30)  return $"il y a {(int)(diff.TotalDays / 7)} semaine(s)";
            return $"il y a {(int)(diff.TotalDays / 30)} mois";
        }

        // ── Modèles formulaire ───────────────────────────────────
        private class CreateDemandeForm
        {
            public string  NomDemande  { get; set; } = string.Empty;
            public string? Description { get; set; }
            public List<LigneForm> Lignes { get; set; } = new() { new LigneForm() };
        }

        private class LigneForm
        {
            public string  Reference   { get; set; } = string.Empty;
            public string  NomProduit  { get; set; } = string.Empty;
            public int     Quantite    { get; set; } = 1;
            public string? Description { get; set; }
            public string  Erreur      { get; set; } = string.Empty;
        }
    }
}