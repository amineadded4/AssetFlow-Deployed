using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using AssetFlow.BlazorUI.DTOs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Pages.Admin
{
    public class ProjectFormModel
    {
        public string    Nom         { get; set; } = string.Empty;
        public string?   Description { get; set; }
        public string    Statut      { get; set; } = "Planifie";
        public string    Priorite    { get; set; } = "Moyenne";
        public string?   Responsable { get; set; }
        public decimal?  Budget      { get; set; }
        public DateTime? DateDebut   { get; set; }
        public DateTime? DateFin     { get; set; }
    }

    public partial class AdminProjects : ComponentBase, IAsyncDisposable
    {
        [Inject] private ProjectClientService ProjectService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        // ── État liste ──
        private List<ProjectDto> Projects     { get; set; } = new();
        private bool             IsLoading    { get; set; } = true;
        private string           ErrorMessage { get; set; } = string.Empty;

        // ── Sidebar ──
        private bool             ShowSidebar  { get; set; } = false;
        private bool             IsEditing    { get; set; } = false;
        private int              EditingId    { get; set; }
        private ProjectFormModel Form         { get; set; } = new();
        private bool             FormLoading  { get; set; } = false;
        private string           FormError    { get; set; } = string.Empty;

        // ── Suppression ──
        private bool        ShowDeleteConfirm { get; set; } = false;
        private ProjectDto? ProjectToDelete   { get; set; }

        // ── Dropdown affectations ── ← NOUVEAU
        private int?                        ExpandedProjectId  { get; set; } = null;
        private List<ProjetAffectationDto>  AffectationsOuvertes { get; set; } = new();
        private bool                        LoadingAffectations { get; set; } = false;
        private HubConnection? _hubConnection;

        protected override async Task OnInitializedAsync()
        {
            await LoadProjectsAsync();
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

            _hubConnection.Reconnected += async _ =>
            {
                try { await _hubConnection.InvokeAsync("JoinDashboard"); } catch { }
                await InvokeAsync(async () =>
                {
                    try { await LoadProjectsAsync(); }
                    catch { }
                    finally { StateHasChanged(); }
                });
            };

            _hubConnection.On("DashboardUpdated", async () =>
            {
                await InvokeAsync(async () =>
                {
                    try
                    {
                        await LoadProjectsAsync();

                        // Resync les affectations si un projet est ouvert
                        if (ExpandedProjectId.HasValue)
                        {
                            AffectationsOuvertes = await ProjectService.GetAffectationsAsync(ExpandedProjectId.Value);
                        }
                    }
                    catch { /* silencieux */ }
                    finally { StateHasChanged(); }
                });
            });

            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinDashboard");
            }
            catch { /* reste statique si SignalR non dispo */ }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                try { await _hubConnection.InvokeAsync("LeaveDashboard"); } catch { }
                await _hubConnection.DisposeAsync();
            }
        }

        private async Task LoadProjectsAsync()
        {
            IsLoading    = true;
            ErrorMessage = string.Empty;
            try   { Projects = await ProjectService.GetAllAsync() ?? new(); }
            catch { ErrorMessage = "Impossible de charger les projets."; }
            finally { IsLoading = false; }
        }

        // ── Toggle dropdown affectations ── ← NOUVEAU
        private async Task ToggleAffectations(int projetId)
        {
            if (ExpandedProjectId == projetId)
            {
                ExpandedProjectId    = null;
                AffectationsOuvertes = new();
                return;
            }

            ExpandedProjectId    = projetId;
            AffectationsOuvertes = new();
            LoadingAffectations  = true;
            StateHasChanged();

            AffectationsOuvertes = await ProjectService.GetAffectationsAsync(projetId);
            LoadingAffectations  = false;
            StateHasChanged();
        }

        // ── Sidebar ──
        private void OpenCreateSidebar()
        {
            Form        = new();
            IsEditing   = false;
            FormError   = string.Empty;
            ShowSidebar = true;
        }

        private void OpenEditSidebar(ProjectDto p)
        {
            EditingId = p.Id;
            Form = new()
            {
                Nom         = p.Nom,
                Description = p.Description,
                Statut      = p.Statut,
                Priorite    = p.Priorite,
                Responsable = p.Responsable,
                Budget      = p.Budget,
                DateDebut   = p.DateDebut,
                DateFin     = p.DateFin
            };
            IsEditing   = true;
            FormError   = string.Empty;
            ShowSidebar = true;
        }

        private void CloseSidebar()
        {
            ShowSidebar = false;
            FormError   = string.Empty;
        }

        private async Task HandleSubmit()
        {
            FormError = string.Empty;
            if (string.IsNullOrWhiteSpace(Form.Nom))
            {
                FormError = "Le nom du projet est obligatoire.";
                return;
            }
            FormLoading = true;
            try
            {
                var response = IsEditing
                    ? await ProjectService.UpdateAsync(EditingId, Form)
                    : await ProjectService.CreateAsync(Form);

                if (response.IsSuccessStatusCode) { CloseSidebar(); await LoadProjectsAsync(); }
                else FormError = "Une erreur est survenue. Vérifiez les champs.";
            }
            catch { FormError = "Erreur réseau. Veuillez réessayer."; }
            finally { FormLoading = false; }
        }

        private void ConfirmDelete(ProjectDto p) { ProjectToDelete = p; ShowDeleteConfirm = true; }
        private void CancelDelete()              { ShowDeleteConfirm = false; ProjectToDelete = null; }

        private async Task ExecuteDelete()
        {
            if (ProjectToDelete == null) return;
            FormLoading = true;
            try
            {
                var r = await ProjectService.DeleteAsync(ProjectToDelete.Id);
                if (r.IsSuccessStatusCode)
                {
                    ShowDeleteConfirm = false;
                    ProjectToDelete   = null;
                    if (ExpandedProjectId == ProjectToDelete?.Id) ExpandedProjectId = null;
                    await LoadProjectsAsync();
                }
            }
            finally { FormLoading = false; }
        }

        private static string GetStatutLabel(string s) => s switch
        {
            "EnCours"  => "En cours", "Planifie" => "Planifié",
            "Suspendu" => "Suspendu", "Termine"  => "Terminé", _ => s
        };
        private static string GetStatutClass(string s) => s switch
        {
            "EnCours"  => "statut-encours",  "Planifie" => "statut-planifie",
            "Suspendu" => "statut-suspendu", "Termine"  => "statut-termine", _ => ""
        };
        private static string GetPrioriteClass(string p) => p switch
        {
            "Critique" => "priorite-critique", "Haute"  => "priorite-haute",
            "Moyenne"  => "priorite-moyenne",  "Faible" => "priorite-faible", _ => ""
        };
    }
}