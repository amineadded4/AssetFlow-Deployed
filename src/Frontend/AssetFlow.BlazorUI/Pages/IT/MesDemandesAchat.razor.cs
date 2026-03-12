// ============================================================
// AssetFlow.BlazorUI / Pages / IT / MesDemandesAchat.razor.cs
// ============================================================
using AssetFlow.Application.DTOs;
using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class MesDemandesAchat
    {
        // ── Injections ───────────────────────────────────────────
        [Inject] private DemandeAchatITClientService DemandeService { get; set; } = default!;
        [Inject] private NavigationManager           Navigation      { get; set; } = default!;
        [Inject] private ILocalStorageService        LocalStorage    { get; set; } = default!;

        // ── État UI ──────────────────────────────────────────────
        private bool   IsLoading      { get; set; } = true;
        private bool   _menuOpen      = false;
        private string UserName       { get; set; } = "IT";
        private string ErrorMessage   { get; set; } = string.Empty;
        private string SuccessMessage { get; set; } = string.Empty;

        // ── Données ──────────────────────────────────────────────
        private List<DemandeAchatITDto> Demandes         { get; set; } = new();
        private List<DemandeAchatITDto> DemandesFiltrees { get; set; } = new();

        private IEnumerable<DemandeAchatITDto> PagedDemandes =>
            DemandesFiltrees.Skip((CurrentPage - 1) * PageSize).Take(PageSize);

        // ── Filtres & tri ────────────────────────────────────────
        private string? FilterStatut      { get; set; } = null;
        private string  SearchQuery       { get; set; } = string.Empty;
        private string  SortOrder         { get; set; } = "date_desc";
        private int?    SelectedDemandeId { get; set; } = null;

        // ── Pagination ───────────────────────────────────────────
        private int CurrentPage { get; set; } = 1;
        private const int PageSize = 10;
        private int TotalPages => Math.Max(1, (int)Math.Ceiling(DemandesFiltrees.Count / (double)PageSize));

        // ── Panneau création ─────────────────────────────────────
        private bool   _showCreatePanel = false;
        private bool   _isSaving        = false;
        private CreateDemandeForm _form = new();
        private Dictionary<string, string> _formErrors = new()
        {
            ["NomProduit"] = string.Empty,
            ["Quantite"]   = string.Empty,
        };

        // ── Init ─────────────────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            UserName = await LocalStorage.GetItemAsync<string>("user_name") ?? "IT";
            await LoadDemandesAsync();
        }

        // ── Chargement données ───────────────────────────────────
        private async Task LoadDemandesAsync()
        {
            IsLoading = true;
            StateHasChanged();
            try
            {
                Demandes = await DemandeService.GetDemandesAsync();
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

        // ── Filtrage & tri ───────────────────────────────────────
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
                    (d.Description?.ToLower().Contains(q) ?? false));
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

        // ── Handlers liste ───────────────────────────────────────
        private void SetFilter(string? statut)
        {
            FilterStatut = statut;
            AppliquerFiltres();
        }

        private void OnSearchInput(ChangeEventArgs e)
        {
            SearchQuery = e.Value?.ToString() ?? string.Empty;
            AppliquerFiltres();
        }

        private void ClearSearch()
        {
            SearchQuery = string.Empty;
            AppliquerFiltres();
        }

        private void OnSortChange(ChangeEventArgs e)
        {
            SortOrder = e.Value?.ToString() ?? "date_desc";
            AppliquerFiltres();
        }

        private void ToggleExpand(int id)
        {
            SelectedDemandeId = SelectedDemandeId == id ? null : id;
        }

        // ── Navigation ───────────────────────────────────────────
        private void NavigateToOffres(int demandeId)
            => Navigation.NavigateTo($"/it/offres/{demandeId}");

        // ── Panneau création ─────────────────────────────────────
        private void OpenCreatePanel()
        {
            _form = new CreateDemandeForm();
            _formErrors["NomProduit"] = string.Empty;
            _formErrors["Quantite"]   = string.Empty;
            ErrorMessage              = string.Empty;
            SuccessMessage            = string.Empty;
            _showCreatePanel          = true;
        }

        private void CloseCreatePanel()
        {
            _showCreatePanel = false;
        }

        private async Task SubmitCreate()
        {
            // Validation
            _formErrors["NomProduit"] = string.IsNullOrWhiteSpace(_form.NomProduit)
                ? "Le nom du produit est obligatoire."
                : string.Empty;

            _formErrors["Quantite"] = _form.Quantite < 1
                ? "La quantité doit être au moins 1."
                : string.Empty;

            if (_formErrors.Values.Any(e => !string.IsNullOrEmpty(e)))
                return;

            _isSaving = true;
            StateHasChanged();

            try
            {
                await DemandeService.CreateDemandeAsync(new CreateDemandeAchatDto
                {
                    NomProduit   = _form.NomProduit.Trim(),
                    Reference    = string.IsNullOrWhiteSpace(_form.Reference) ? null : _form.Reference.Trim(),
                    Quantite     = _form.Quantite,
                    Description  = _form.Description?.Trim(),
                    DemandeurNom = UserName
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

        // ── Pagination ───────────────────────────────────────────
        private void PrevPage()         { if (CurrentPage > 1)         CurrentPage--; }
        private void NextPage()         { if (CurrentPage < TotalPages) CurrentPage++; }
        private void GoToPage(int page) => CurrentPage = page;

        // ── Helpers ──────────────────────────────────────────────
        private string GetInitials()
        {
            var parts = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
            return "IT";
        }

        private static string GetStatutLabel(string statut) => statut switch
        {
            "en_attente" => "En attente",
            "traite"     => "Traité",
            "approuve"   => "Approuvé",
            "refuse"     => "Refusé",
            _            => statut
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
            public string  NomProduit  { get; set; } = string.Empty;
            public string? Reference   { get; set; }
            public int     Quantite    { get; set; } = 1;
            public string? Description { get; set; }
        }
    }
}
