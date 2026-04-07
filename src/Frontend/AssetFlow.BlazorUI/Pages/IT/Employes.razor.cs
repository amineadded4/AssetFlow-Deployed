using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using AssetFlow.BlazorUI.DTOs;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class Employes : IAsyncDisposable
    {
        [Inject] private EmployeManagementService    Svc              { get; set; } = default!;
        [Inject] private NotificationService   NotifSvc         { get; set; } = default!;
        [Inject] private ILocalStorageService        LocalStorage     { get; set; } = default!;
        [Inject] private NavigationManager           Navigation       { get; set; } = default!;
        [Inject] private VoiceCommandService         VoiceSvc         { get; set; } = default!;
        [Inject] private IJSRuntime                  JS               { get; set; } = default!;

        // ── Mode toggle ──
        private bool ModeProjet { get; set; } = false;

        // ── Employés ──
        private List<EmployeListeDto> _allEmployees = new();
        private List<EmployeListeDto> Employees
        {
            get
            {
                var q = _allEmployees.AsEnumerable();
                if (FiltreRoleEmp != "tous")
                    q = q.Where(u => u.Role.Equals(FiltreRoleEmp, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(Search))
                {
                    var s = Search.Trim().ToLower();
                    q = q.Where(u =>
                        u.FullName.ToLower().Contains(s) ||
                        u.Email.ToLower().Contains(s)    ||
                        u.Role.ToLower().Contains(s));
                }
                return q.ToList();
            }
        }
        private EmployeListeDto?            EmployeSelectionne  { get; set; }
        private List<AffectationEmployeDto> Affectations        { get; set; } = new();
        private string FiltreRoleEmp { get; set; } = "tous";

        // ── Projets ──
        private List<ProjetAffectationListeDto> Projets             { get; set; } = new();
        private ProjetAffectationListeDto?      ProjetSelectionne   { get; set; }
        private List<AffectationEmployeDto>     AffectationsProjets { get; set; } = new();
        private string                          ProjetSearch        { get; set; } = string.Empty;
        private bool                            LoadingProjets      { get; set; } = false;

        // ── Communs ──
        private AffectationEmployeDto? AffectationARetirer { get; set; }
        private string  Search              { get; set; } = string.Empty;
        private string  FiltreEtat          { get; set; } = "tous";
        private string  UserName            { get; set; } = "IT";
        private string  SuccessMsg          { get; set; } = string.Empty;
        private string  ErrorMsg            { get; set; } = string.Empty;
        private bool    LoadingEmployes     { get; set; } = true;
        private bool    LoadingAffectations { get; set; } = false;
        private bool    IsRetiring          { get; set; } = false;

        // ── Notifications ──
        private bool                    _notifOpen    = false;
        private bool                    _loadingNotifs = false;
        private int                     _nbNonLues    = 0;
        private List<NotificationDto>   _notifications = new();
        private System.Timers.Timer?    _notifTimer;

        private System.Timers.Timer? _debounce;
        private bool _menuOpen = false;
        private string _roleUtilisateur = "Service IT";
        private bool _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        // ── Dark Mode ──
        private bool _isDark = false;

        // ── Computed ──
        private List<AffectationEmployeDto> AffectationsCourantes =>
            ModeProjet ? AffectationsProjets : Affectations;

        private List<AffectationEmployeDto> AffectationsFiltrees =>
            FiltreEtat == "tous"
                ? AffectationsCourantes
                : AffectationsCourantes.Where(a => a.Etat == FiltreEtat).ToList();

        private int NbActives =>
            AffectationsCourantes.Count(a => a.Etat == "Courante");

        // ── Init ──
        protected override async Task OnInitializedAsync()
        {
            VoiceSvc.OnCommand += HandleVoiceCommand;
            UserName         = await LocalStorage.GetItemAsync<string>("user_name") ?? "IT";
            _roleUtilisateur = await LocalStorage.GetItemAsync<string>("user_role") ?? "IT";

            // Restaurer le dark mode depuis localStorage
            _isDark = await LocalStorage.GetItemAsync<bool>("darkMode");
            await ApplyDark(_isDark);

            await LoadEmployesAsync();
            await RefreshCountAsync();

            // Actualiser le compteur toutes les 60 secondes
            _notifTimer = new System.Timers.Timer(60_000);
            _notifTimer.Elapsed += async (_, _) =>
            {
                await InvokeAsync(async () =>
                {
                    await RefreshCountAsync();
                    StateHasChanged();
                });
            };
            _notifTimer.AutoReset = true;
            _notifTimer.Start();
        }

        public async ValueTask DisposeAsync()
        {
            VoiceSvc.OnCommand -= HandleVoiceCommand;
            _notifTimer?.Stop();
            _notifTimer?.Dispose();
        }

        // ── Dark Mode ──
        private async Task ToggleDark()
        {
            _isDark = !_isDark;
            await LocalStorage.SetItemAsync("darkMode", _isDark);
            await ApplyDark(_isDark);
        }

        private async Task ApplyDark(bool dark)
        {
            await JS.InvokeVoidAsync("eval",
                dark ? "document.documentElement.classList.add('dark')"
                     : "document.documentElement.classList.remove('dark')");
        }

        // ── Notifications ──
        private async Task RefreshCountAsync()
        {
            _nbNonLues = await NotifSvc.GetNombreNonLuesAsync();
        }

        private async Task ToggleNotifications()
        {
            _notifOpen = !_notifOpen;
            if (_notifOpen && !_notifications.Any())
                await LoadNotificationsAsync();
        }

        private async Task LoadNotificationsAsync()
        {
            _loadingNotifs = true;
            StateHasChanged();
            _notifications = await NotifSvc.GetNotificationsAsync();
            _loadingNotifs = false;
            StateHasChanged();
        }

        private async Task MarquerLue(NotificationDto notif)
        {
            if (notif.EstLue) return;
            notif.EstLue = true; // Optimistic update
            _nbNonLues   = Math.Max(0, _nbNonLues - 1);
            await NotifSvc.MarquerCommeLueAsync(notif.Id);
            StateHasChanged();
        }

        private async Task MarquerToutesLues()
        {
            await NotifSvc.MarquerToutesCommeLuesAsync();
            foreach (var n in _notifications) n.EstLue = true;
            _nbNonLues = 0;
            StateHasChanged();
        }

        private static string GetNiveauClass(string niveau) => niveau switch
        {
            "Critique"      => "critique",
            "Avertissement" => "avertissement",
            _               => "info"
        };

        // ── Voice ──
        private Task HandleVoiceCommand(VoiceCommand cmd)
        {
            return InvokeAsync(async () =>
            {
                switch (cmd.Type)
                {
                    case VoiceCommandType.SélectionnerEmploye when string.IsNullOrWhiteSpace(cmd.Designation):
                        await SetMode(false); break;
                    case VoiceCommandType.SélectionnerProjet when string.IsNullOrWhiteSpace(cmd.Designation):
                        await SetMode(true);  break;
                    case VoiceCommandType.SélectionnerEmploye:
                    {
                        await SetMode(false);
                        var e = TrouverEmploye(cmd.Designation);
                        if (e != null) await SelectEmploye(e);
                        else ErrorMsg = $"Employé '{cmd.Designation}' introuvable.";
                        break;
                    }
                    case VoiceCommandType.SélectionnerProjet:
                    {
                        await SetMode(true);
                        var p = TrouverProjet(cmd.Designation);
                        if (p != null) await SelectProjet(p);
                        else ErrorMsg = $"Projet '{cmd.Designation}' introuvable.";
                        break;
                    }
                    case VoiceCommandType.RévoquerAffectation:
                    {
                        if (EmployeSelectionne == null && ProjetSelectionne == null)
                        { ErrorMsg = "Sélectionnez d'abord un employé ou un projet."; break; }
                        var a = TrouverAffectation(cmd.Designation ?? cmd.Reference);
                        if (a != null) DemanderConfirmation(a);
                        else ErrorMsg = $"Affectation '{cmd.Designation ?? cmd.Reference}' introuvable.";
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(ErrorMsg))
                {
                    StateHasChanged();
                    await Task.Delay(3000);
                    ErrorMsg = string.Empty;
                }
                StateHasChanged();
            });
        }

        // ── Helpers recherche vocale ──
        private ProjetAffectationListeDto? TrouverProjet(string? designation)
        {
            if (string.IsNullOrWhiteSpace(designation)) return null;
            var exact = Projets.FirstOrDefault(p => p.Nom.Equals(designation, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;
            var terms = designation.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return Projets.Select(p => new { Projet = p, Score = terms.Count(t => p.Nom.ToLower().Contains(t)) })
                          .Where(x => x.Score > 0).OrderByDescending(x => x.Score)
                          .Select(x => x.Projet).FirstOrDefault();
        }
        private EmployeListeDto? TrouverEmploye(string? designation)
        {
            if (string.IsNullOrWhiteSpace(designation)) return null;
            var exact = _allEmployees.FirstOrDefault(e => e.FullName.Equals(designation, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;
            var terms = designation.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return _allEmployees.Select(e => new { Employe = e, Score = terms.Count(t => e.FullName.ToLower().Contains(t)) })
                                .Where(x => x.Score > 0).OrderByDescending(x => x.Score)
                                .Select(x => x.Employe).FirstOrDefault();
        }
        private AffectationEmployeDto? TrouverAffectation(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var d = input.ToLower().Trim();
            var liste = ModeProjet ? AffectationsProjets : Affectations;
            return liste.FirstOrDefault(a => a.Designation.ToLower().Contains(d) || a.Reference.ToLower().Contains(d));
        }

        // ── Mode toggle ──
        private async Task SetMode(bool projet)
        {
            ModeProjet          = projet;
            EmployeSelectionne  = null;
            ProjetSelectionne   = null;
            Affectations        = new();
            AffectationsProjets = new();
            FiltreEtat          = "tous";
            SuccessMsg          = string.Empty;
            ErrorMsg            = string.Empty;
            if (projet && !Projets.Any()) await LoadProjetsAsync();
        }

        private async Task LoadEmployesAsync(string? search = null)
        {
            LoadingEmployes = true;
            StateHasChanged();
            _allEmployees   = await Svc.GetEmployesAsync(search);
            LoadingEmployes = false;
            StateHasChanged();
        }
        private void OnSearchInput(ChangeEventArgs e) => Search = e.Value?.ToString() ?? string.Empty;

        private async Task SelectEmploye(EmployeListeDto emp)
        {
            EmployeSelectionne  = emp;
            FiltreEtat          = "tous";
            SuccessMsg = ErrorMsg = string.Empty;
            LoadingAffectations = true;
            StateHasChanged();
            Affectations        = await Svc.GetAffectationsAsync(emp.Id);
            LoadingAffectations = false;
            StateHasChanged();
        }

        private async Task LoadProjetsAsync(string? search = null)
        {
            LoadingProjets = true;
            StateHasChanged();
            Projets        = await Svc.GetProjetsAvecAffectationsAsync(search);
            LoadingProjets = false;
            StateHasChanged();
        }
        private void OnProjetSearchInput(ChangeEventArgs e)
        {
            ProjetSearch = e.Value?.ToString() ?? string.Empty;
            _debounce?.Stop();
            _debounce = new System.Timers.Timer(350);
            _debounce.Elapsed += async (_, _) =>
            {
                _debounce?.Stop();
                await InvokeAsync(() => LoadProjetsAsync(ProjetSearch));
            };
            _debounce.AutoReset = false;
            _debounce.Start();
        }
        private async Task SelectProjet(ProjetAffectationListeDto projet)
        {
            ProjetSelectionne   = projet;
            FiltreEtat          = "tous";
            SuccessMsg = ErrorMsg = string.Empty;
            LoadingAffectations = true;
            StateHasChanged();
            AffectationsProjets = await Svc.GetAffectationsProjetAsync(projet.Id);
            LoadingAffectations = false;
            StateHasChanged();
        }

        private void DemanderConfirmation(AffectationEmployeDto aff) => AffectationARetirer = aff;
        private void AnnulerConfirmation()                           => AffectationARetirer = null;

        private async Task ConfirmerRetrait()
        {
            if (AffectationARetirer == null) return;
            IsRetiring = true;
            SuccessMsg = ErrorMsg = string.Empty;
            StateHasChanged();
            var (succes, message) = await Svc.RetirerAffectationAsync(AffectationARetirer.AffectationId);
            IsRetiring          = false;
            AffectationARetirer = null;
            if (succes)
            {
                SuccessMsg = message;
                if (!ModeProjet && EmployeSelectionne != null)
                {
                    Affectations = await Svc.GetAffectationsAsync(EmployeSelectionne.Id);
                    await LoadEmployesAsync(Search);
                }
                else if (ModeProjet && ProjetSelectionne != null)
                {
                    AffectationsProjets = await Svc.GetAffectationsProjetAsync(ProjetSelectionne.Id);
                    await LoadProjetsAsync(ProjetSearch);
                }
                // Rafraîchir les notifications après retrait
                _notifications.Clear();
                await RefreshCountAsync();
            }
            else { ErrorMsg = message; }
            StateHasChanged();
        }

        private static string GetStatutClass(string statut) => statut switch
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
        private string GetInitials()
        {
            var parts = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
            return "IT";
        }
    }
}