// ============================================================
// AssetFlow.BlazorUI / Pages / IT / Affectation.razor.cs
// Logique de la page d'affectation de matériel
// ============================================================

using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class Affectation
    {
        [Inject] private AffectationClientService AffectationService { get; set; } = default!;
        [Inject] private NavigationManager        Navigation          { get; set; } = default!;
        [Inject] private ILocalStorageService     LocalStorage        { get; set; } = default!;

        // ── Mode ───────────────────────────────────────────────
        private bool ModeProjet { get; set; } = false;

        // ── Données matériels ──────────────────────────────────
        private List<MaterielDisponibleDto>  MaterielsDisponibles { get; set; } = new();
        private MaterielDisponibleDto?       MaterielSelectionne  { get; set; } = null;
        private HashSet<int>                 ArticlesSelectionnes { get; set; } = new();
        private string                       MaterielSearch       { get; set; } = string.Empty;
        private bool                         LoadingMateriels     { get; set; } = false;
        private System.Timers.Timer?         _materielDebounce;
        private bool                          _menuOpen = false;

        // ── Données utilisateurs ───────────────────────────────
        private List<UtilisateurDisponibleDto> UtilisateursDisponibles { get; set; } = new();
        private List<UtilisateurDisponibleDto> UtilisateursFiltres     { get; set; } = new();
        private UtilisateurDisponibleDto?      UtilisateurSelectionne  { get; set; } = null;
        private string                         EmployeSearch           { get; set; } = string.Empty;
        private bool                           LoadingUtilisateurs     { get; set; } = false;

        // ── Formulaire ─────────────────────────────────────────
        private string Commentaire    { get; set; } = string.Empty;
        private DateTime? DateRetourPrevue { get; set; }
        private bool   IsSubmitting   { get; set; } = false;
        private string SuccessMessage { get; set; } = string.Empty;
        private string ErrorMessage   { get; set; } = string.Empty;

        // ── User info ──────────────────────────────────────────
        private string UserName { get; set; } = "Utilisateur";

        // ── Computed ───────────────────────────────────────────
        private bool CanConfirm =>
            MaterielSelectionne != null &&
            ArticlesSelectionnes.Any()  &&
            UtilisateurSelectionne != null;

        // ── Init ───────────────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            UserName = await LocalStorage.GetItemAsync<string>("user_name") ?? "IT";
            await Task.WhenAll(
                LoadMaterielsAsync(),
                LoadUtilisateursAsync()
            );
        }

        // ── Mode toggle ────────────────────────────────────────
        private void SetMode(bool projet) => ModeProjet = projet;

        // ── Matériels ──────────────────────────────────────────
        private async Task LoadMaterielsAsync(string? search = null)
        {
            LoadingMateriels = true;
            StateHasChanged();
            MaterielsDisponibles = await AffectationService.GetMaterielsAsync(search);
            LoadingMateriels     = false;
            StateHasChanged();
        }

        private void OnMaterielSearchInput(ChangeEventArgs e)
        {
            MaterielSearch = e.Value?.ToString() ?? string.Empty;
            _materielDebounce?.Stop();
            _materielDebounce = new System.Timers.Timer(350);
            _materielDebounce.Elapsed += async (_, _) =>
            {
                _materielDebounce?.Stop();
                await InvokeAsync(() => LoadMaterielsAsync(MaterielSearch));
            };
            _materielDebounce.AutoReset = false;
            _materielDebounce.Start();
        }

        private void SelectMateriel(MaterielDisponibleDto mat)
        {
            MaterielSelectionne  = mat;
            ArticlesSelectionnes = new HashSet<int>();
            ErrorMessage         = string.Empty;
        }

        private void DeselectMateriel()
        {
            MaterielSelectionne  = null;
            ArticlesSelectionnes = new HashSet<int>();
        }

        private void ToggleArticle(int articleId)
        {
            if (ArticlesSelectionnes.Contains(articleId))
                ArticlesSelectionnes.Remove(articleId);
            else
                ArticlesSelectionnes.Add(articleId);
        }

        // ── Utilisateurs ───────────────────────────────────────
        private async Task LoadUtilisateursAsync()
        {
            LoadingUtilisateurs     = true;
            StateHasChanged();
            UtilisateursDisponibles = await AffectationService.GetUtilisateursAsync();
            UtilisateursFiltres     = UtilisateursDisponibles;
            LoadingUtilisateurs     = false;
            StateHasChanged();
        }

        private void OnEmployeSearchInput(ChangeEventArgs e)
        {
            EmployeSearch = e.Value?.ToString() ?? string.Empty;
            FiltrerUtilisateurs();
        }

        private void FiltrerUtilisateurs()
        {
            if (string.IsNullOrWhiteSpace(EmployeSearch))
            {
                UtilisateursFiltres = UtilisateursDisponibles;
                return;
            }
            var q = EmployeSearch.Trim().ToLower();
            UtilisateursFiltres = UtilisateursDisponibles
                .Where(u =>
                    u.FullName.ToLower().Contains(q) ||
                    u.Department.ToLower().Contains(q) ||
                    u.Email.ToLower().Contains(q))
                .ToList();
        }

        private void SelectUtilisateur(UtilisateurDisponibleDto user)
        {
            UtilisateurSelectionne = UtilisateurSelectionne?.Id == user.Id ? null : user;
            ErrorMessage           = string.Empty;
        }

        // ── Confirmation ───────────────────────────────────────
        private async Task ConfirmerAffectation()
        {
            if (!CanConfirm) return;

            IsSubmitting   = true;
            SuccessMessage = string.Empty;
            ErrorMessage   = string.Empty;
            StateHasChanged();

            var request = new CreerAffectationRequest
            {
                MaterielId    = MaterielSelectionne!.Id,
                UtilisateurId = UtilisateurSelectionne!.Id,
                ArticleIds    = ArticlesSelectionnes.ToList(),
                Observations  = string.IsNullOrWhiteSpace(Commentaire) ? null : Commentaire.Trim(),
                DateRetourPrevue  = DateRetourPrevue
            };

            var result = await AffectationService.CreerAffectationAsync(request);

            IsSubmitting = false;

            if (result.Succes)
            {
                SuccessMessage       = result.Message;
                // Reset form
                MaterielSelectionne  = null;
                UtilisateurSelectionne = null;
                ArticlesSelectionnes = new HashSet<int>();
                Commentaire          = string.Empty;
                DateRetourPrevue = null;
                // Recharger les matériels pour mettre à jour les stocks
                await LoadMaterielsAsync(MaterielSearch);
            }
            else
            {
                ErrorMessage = result.Message;
            }

            StateHasChanged();
        }

        // ── Helpers ────────────────────────────────────────────
        private string GetInitials()
        {
            var parts = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
            return "IT";
        }

        public void Dispose()
        {
            _materielDebounce?.Dispose();
        }
    }
}