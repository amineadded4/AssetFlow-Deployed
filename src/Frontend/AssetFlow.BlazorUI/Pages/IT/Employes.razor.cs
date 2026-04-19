using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using AssetFlow.BlazorUI.DTOs;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.SignalR.Client;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class Employes : IAsyncDisposable
    {
        [Inject] private EmployeManagementService    Svc              { get; set; } = default!;
        [Inject] private NotificationService   NotifSvc         { get; set; } = default!;
        [Inject] private ILocalStorageService        LocalStorage     { get; set; } = default!;
        [Inject] private NavigationManager           Navigation       { get; set; } = default!;
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
        private HubConnection?       _hubConnection;

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

            // Après reconnexion → rejoindre le groupe + resynchroniser
            _hubConnection.Reconnected += async _ =>
            {
                try { await _hubConnection.InvokeAsync("JoinDashboard"); } catch { }
                await InvokeAsync(async () =>
                {
                    try
                    {
                        await LoadEmployesAsync(Search);
                        if (ModeProjet && Projets.Any())
                            await LoadProjetsAsync(ProjetSearch);
                        // Resync affectations de la sélection active
                        if (!ModeProjet && EmployeSelectionne != null)
                            Affectations = await Svc.GetAffectationsAsync(EmployeSelectionne.Id);
                        else if (ModeProjet && ProjetSelectionne != null)
                            AffectationsProjets = await Svc.GetAffectationsProjetAsync(ProjetSelectionne.Id);
                        await RefreshCountAsync();
                    }
                    catch { }
                    finally { StateHasChanged(); }
                });
            };

            // Affectation créée / retirée / statut changé → recharger
            _hubConnection.On("DashboardUpdated", async () =>
            {
                await InvokeAsync(async () =>
                {
                    try
                    {
                        await LoadEmployesAsync(Search);

                        if (ModeProjet && Projets.Any())
                            await LoadProjetsAsync(ProjetSearch);

                        // Resync la liste d'affectations si un employé/projet est sélectionné
                        if (!ModeProjet && EmployeSelectionne != null)
                            Affectations = await Svc.GetAffectationsAsync(EmployeSelectionne.Id);
                        else if (ModeProjet && ProjetSelectionne != null)
                            AffectationsProjets = await Svc.GetAffectationsProjetAsync(ProjetSelectionne.Id);

                        await RefreshCountAsync();
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