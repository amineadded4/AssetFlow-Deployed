using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using AssetFlow.BlazorUI.DTOs;

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

    public partial class AdminProjects : Microsoft.AspNetCore.Components.ComponentBase
    {
        [Inject] private ProjectClientService ProjectService { get; set; } = default!;

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

        protected override async Task OnInitializedAsync()
            => await LoadProjectsAsync();

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