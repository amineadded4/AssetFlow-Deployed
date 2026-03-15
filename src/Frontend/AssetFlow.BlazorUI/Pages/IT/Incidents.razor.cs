using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class Incidents
    {
        [Inject] private ITIncidentService    Svc          { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage  { get; set; } = default!;

        private List<IncidentEmployeDto> _allEmployes = new();
        private string FiltreRole { get; set; } = "tous";
        private List<IncidentEmployeDto> Employes
        {
            get
            {
                var q = _allEmployes.AsEnumerable();

                if (FiltreRole != "tous")
                    q = q.Where(u => u.Role.Equals(FiltreRole, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(Search))
                {
                    var s = Search.Trim().ToLower();
                    q = q.Where(u =>
                        u.FullName.ToLower().Contains(s) ||
                        u.Role.ToLower().Contains(s));
                }

                return q.ToList();
            }
        }
        private IncidentEmployeDto?       EmployeSelectionne  { get; set; }
        private List<IncidentMaterielDto> Materiels           { get; set; } = new();
        private IncidentMaterielDto?      MaterielSelectionne { get; set; }

        private string Search        { get; set; } = string.Empty;
        private string FiltreStatut  { get; set; } = "actifs";
        private string UserName      { get; set; } = "IT";
        private string SuccessMsg    { get; set; } = string.Empty;
        private string ErrorMsg      { get; set; } = string.Empty;
        private bool   LoadingEmployes  { get; set; } = true;
        private bool   LoadingMateriels { get; set; } = false;
        private bool   IsProcessing     { get; set; } = false;

        // Modals
        private IncidentItemDto?    ModalIncident          { get; set; }
        private IncidentArticleDto? ModalArticleResolveAll { get; set; }
        private IncidentArticleDto? ModalArticleContext     { get; set; }
        private string              ModalCommentaire       { get; set; } = string.Empty;

        private System.Timers.Timer? _debounce;
        private bool _menuOpen = false;

        protected override async Task OnInitializedAsync()
        {
            UserName = await LocalStorage.GetItemAsync<string>("user_name") ?? "IT";
            await LoadEmployesAsync();
        }

        private async Task LoadEmployesAsync(string? search = null)
        {
            LoadingEmployes = true; StateHasChanged();
            _allEmployes   = await Svc.GetEmployesAsync(search);
            LoadingEmployes = false; StateHasChanged();
        }

        private void OnSearchInput(ChangeEventArgs e)
        {
            Search = e.Value?.ToString() ?? string.Empty;
        }

        private async Task SelectEmploye(IncidentEmployeDto emp)
        {
            EmployeSelectionne  = emp;
            MaterielSelectionne = null;
            LoadingMateriels    = true; StateHasChanged();
            Materiels           = await Svc.GetMaterielsAsync(emp.UtilisateurId);
            LoadingMateriels    = false; StateHasChanged();
        }

        private void SelectMateriel(IncidentMaterielDto mat) => MaterielSelectionne = mat;

        // ── Actions statut ──
        private async Task ChangerStatut(int incidentId, IncidentArticleDto art, string statut)
        {
            IsProcessing = true; StateHasChanged();
            var (ok, msg) = await Svc.ChangerStatutAsync(incidentId, statut);
            if (ok) { SuccessMsg = msg; await RefreshMateriels(); }
            else      ErrorMsg  = msg;
            IsProcessing = false; StateHasChanged();
        }

        private void OuvrirModalResolu(IncidentItemDto inc, IncidentArticleDto art)
        {
            ModalIncident        = inc;
            ModalArticleContext  = art;
            ModalCommentaire     = string.Empty;
        }

        private void OuvrirModalResolveAll(IncidentArticleDto art)
        {
            ModalArticleResolveAll = art;
            ModalCommentaire       = string.Empty;
        }

        private void FermerModal()
        {
            ModalIncident          = null;
            ModalArticleResolveAll = null;
            ModalArticleContext    = null;
            ModalCommentaire       = string.Empty;
        }

        private async Task ConfirmerResolu()
        {
            if (ModalIncident == null) return;
            IsProcessing = true; StateHasChanged();
            var (ok, msg) = await Svc.ChangerStatutAsync(ModalIncident.Id, "Resolu", ModalCommentaire);
            FermerModal();
            if (ok) { SuccessMsg = msg; await RefreshMateriels(); }
            else      ErrorMsg  = msg;
            IsProcessing = false; StateHasChanged();
        }

        private async Task ConfirmerResolveAll()
        {
            if (ModalArticleResolveAll == null) return;
            IsProcessing = true; StateHasChanged();
            var (ok, msg) = await Svc.ResolveAllByArticleAsync(ModalArticleResolveAll.ArticleId, ModalCommentaire);
            FermerModal();
            if (ok) { SuccessMsg = msg; await RefreshMateriels(); }
            else      ErrorMsg  = msg;
            IsProcessing = false; StateHasChanged();
        }

        private async Task RefreshMateriels()
        {
            if (EmployeSelectionne == null) return;
            Materiels = await Svc.GetMaterielsAsync(EmployeSelectionne.UtilisateurId);
            // Resynchroniser le matériel sélectionné
            if (MaterielSelectionne != null)
                MaterielSelectionne = Materiels.FirstOrDefault(m => m.MaterielId == MaterielSelectionne.MaterielId);
            await LoadEmployesAsync(Search); // refresh compteurs
        }

        // ── Helpers CSS ──
        private string GetStatutClass(string statut) => statut switch
        {
            "EnAttente" => "en-attente",
            "EnCours"   => "en-cours",
            "Resolu"    => "resolu",
            "Cloture"   => "cloture",
            _           => ""
        };

        private string GetUrgenceClass(int urgence) =>
            urgence <= 33 ? "faible" : urgence <= 66 ? "moyen" : "critique";

        private string GetInitials()
        {
            var p = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (p.Length >= 2) return $"{p[0][0]}{p[1][0]}".ToUpper();
            if (p.Length == 1 && p[0].Length >= 2) return p[0][..2].ToUpper();
            return "IT";
        }
    }
}