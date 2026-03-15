// ============================================================
// AssetFlow.BlazorUI / Pages / IT / Affectation.razor.cs
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

        // ── Mode ──────────────────────────────────────────────
        private bool ModeProjet { get; set; } = false;

        // ── Matériels ─────────────────────────────────────────
        private List<MaterielDisponibleDto> MaterielsDisponibles { get; set; } = new();
        private MaterielDisponibleDto?      MaterielSelectionne  { get; set; } = null;
        private HashSet<int>                ArticlesSelectionnes { get; set; } = new();
        private string                      MaterielSearch       { get; set; } = string.Empty;
        private bool                        LoadingMateriels     { get; set; } = false;
        private System.Timers.Timer?        _materielDebounce;
        private bool                        _menuOpen            = false;

        // ── Utilisateurs ──────────────────────────────────────
        private List<UtilisateurDisponibleDto> UtilisateursDisponibles { get; set; } = new();
        private UtilisateurDisponibleDto?      UtilisateurSelectionne  { get; set; } = null;
        private string                         EmployeSearch           { get; set; } = string.Empty;
        private bool                           LoadingUtilisateurs     { get; set; } = false;
        private string                         FiltreRole              { get; set; } = "tous";

        // ── Propriété calculée : filtre role + search ──────────
        private List<UtilisateurDisponibleDto> UtilisateursFiltres
        {
            get
            {
                var q = UtilisateursDisponibles.AsEnumerable();

                if (FiltreRole != "tous")
                    q = q.Where(u => u.Role.Equals(FiltreRole, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(EmployeSearch))
                {
                    var s = EmployeSearch.Trim().ToLower();
                    q = q.Where(u =>
                        u.FullName.ToLower().Contains(s) ||
                        u.Role.ToLower().Contains(s) ||
                        u.Email.ToLower().Contains(s));
                }

                return q.ToList();
            }
        }

        // ── Projets ───────────────────────────────────────────
        private List<ProjetDisponibleDto> ProjetsDisponibles { get; set; } = new();
        private List<ProjetDisponibleDto> ProjetsFiltres     { get; set; } = new();
        private ProjetDisponibleDto?      ProjetSelectionne  { get; set; } = null;
        private string                    ProjetSearch       { get; set; } = string.Empty;
        private bool                      LoadingProjets     { get; set; } = false;

        // ── Formulaire ────────────────────────────────────────
        private string    Commentaire      { get; set; } = string.Empty;
        private DateTime? DateRetourPrevue { get; set; }
        private bool      IsSubmitting     { get; set; } = false;
        private string    SuccessMessage   { get; set; } = string.Empty;
        private string    ErrorMessage     { get; set; } = string.Empty;

        // ── User info ─────────────────────────────────────────
        private string UserName { get; set; } = "Utilisateur";

        // ── Computed ──────────────────────────────────────────
        private bool BeneficiaireOk => ModeProjet
            ? ProjetSelectionne != null
            : UtilisateurSelectionne != null;

        private bool CanConfirm =>
            MaterielSelectionne != null &&
            ArticlesSelectionnes.Any()  &&
            BeneficiaireOk;

        // ── Init ──────────────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            UserName = await LocalStorage.GetItemAsync<string>("user_name") ?? "IT";
            await Task.WhenAll(
                LoadMaterielsAsync(),
                LoadUtilisateursAsync(),
                LoadProjetsAsync()
            );
        }

        // ── Mode toggle ───────────────────────────────────────
        private void SetMode(bool projet)
        {
            ModeProjet             = projet;
            UtilisateurSelectionne = null;
            ProjetSelectionne      = null;
            ErrorMessage           = string.Empty;
        }

        // ── Matériels ─────────────────────────────────────────
        private async Task LoadMaterielsAsync(string? search = null)
        {
            LoadingMateriels     = true;
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

        // ── Utilisateurs ──────────────────────────────────────
        private async Task LoadUtilisateursAsync()
        {
            LoadingUtilisateurs     = true;
            StateHasChanged();
            UtilisateursDisponibles = await AffectationService.GetUtilisateursAsync();
            LoadingUtilisateurs     = false;
            StateHasChanged();
        }

        private void OnEmployeSearchInput(ChangeEventArgs e)
        {
            EmployeSearch = e.Value?.ToString() ?? string.Empty;
        }

        private void SelectUtilisateur(UtilisateurDisponibleDto user)
        {
            UtilisateurSelectionne = UtilisateurSelectionne?.Id == user.Id ? null : user;
            ErrorMessage           = string.Empty;
        }

        // ── Projets ───────────────────────────────────────────
        private async Task LoadProjetsAsync()
        {
            LoadingProjets     = true;
            StateHasChanged();
            ProjetsDisponibles = await AffectationService.GetProjetsAsync();
            ProjetsFiltres     = ProjetsDisponibles;
            LoadingProjets     = false;
            StateHasChanged();
        }

        private void OnProjetSearchInput(ChangeEventArgs e)
        {
            ProjetSearch = e.Value?.ToString() ?? string.Empty;
            FiltrerProjets();
        }

        private void FiltrerProjets()
        {
            if (string.IsNullOrWhiteSpace(ProjetSearch))
            {
                ProjetsFiltres = ProjetsDisponibles;
                return;
            }
            var q = ProjetSearch.Trim().ToLower();
            ProjetsFiltres = ProjetsDisponibles
                .Where(p =>
                    p.Nom.ToLower().Contains(q) ||
                    (p.Responsable ?? "").ToLower().Contains(q))
                .ToList();
        }

        private void SelectProjet(ProjetDisponibleDto projet)
        {
            ProjetSelectionne = ProjetSelectionne?.Id == projet.Id ? null : projet;
            ErrorMessage      = string.Empty;
        }

        // ── Confirmation ──────────────────────────────────────
        private async Task ConfirmerAffectation()
        {
            if (!CanConfirm) return;

            IsSubmitting   = true;
            SuccessMessage = string.Empty;
            ErrorMessage   = string.Empty;
            StateHasChanged();

            var request = new CreerAffectationRequest
            {
                MaterielId       = MaterielSelectionne!.Id,
                UtilisateurId    = ModeProjet ? null : UtilisateurSelectionne!.Id,
                ProjetId         = ModeProjet ? ProjetSelectionne!.Id : null,
                ArticleIds       = ArticlesSelectionnes.ToList(),
                Observations     = string.IsNullOrWhiteSpace(Commentaire) ? null : Commentaire.Trim(),
                DateRetourPrevue = DateRetourPrevue
            };

            var result = await AffectationService.CreerAffectationAsync(request);

            IsSubmitting = false;

            if (result.Succes)
            {
                SuccessMessage         = result.Message;
                MaterielSelectionne    = null;
                UtilisateurSelectionne = null;
                ProjetSelectionne      = null;
                ArticlesSelectionnes   = new HashSet<int>();
                Commentaire            = string.Empty;
                DateRetourPrevue       = null;
                await LoadMaterielsAsync(MaterielSearch);
            }
            else
            {
                ErrorMessage = result.Message;
            }

            StateHasChanged();
        }

        // ── Helpers ───────────────────────────────────────────
        private string GetInitials()
        {
            var parts = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
            return "IT";
        }

        private static string GetStatutProjetClass(string statut) => statut switch
        {
            "EnCours"  => "statut-encours",
            "Planifie" => "statut-planifie",
            "Suspendu" => "statut-suspendu",
            _          => ""
        };

        private static string GetPrioriteClass(string priorite) => priorite switch
        {
            "Critique" => "priorite-critique",
            "Haute"    => "priorite-haute",
            "Moyenne"  => "priorite-moyenne",
            "Faible"   => "priorite-faible",
            _          => ""
        };

        public void Dispose() => _materielDebounce?.Dispose();
    }
}