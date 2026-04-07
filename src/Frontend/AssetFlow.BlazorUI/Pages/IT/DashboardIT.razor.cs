using AssetFlow.BlazorUI.DTOs;
using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.SignalR.Client;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class DashboardIT : ComponentBase, IAsyncDisposable
    {
        [Inject] private IJSRuntime         JS      { get; set; } = default!;
        [Inject] private StatistiquesITService StatSvc { get; set; } = default!;
        [Inject] private VoiceCommandService VoiceSvc { get; set; } = default!;

        // ─── UI ──────────────────────────────────────────────────
        private string _theme           = "dark";
        private bool   _sidebarOpen     = false;
        private string _nomUtilisateur  = "Agent IT";
        private string _roleUtilisateur = "IT";
        private bool _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        private string _initiales       = "IT";
        private bool   _chargement      = true;
        private string _lastUpdate      = "--:--";

        private string _toastMsg  = string.Empty;
        private string _toastType = "it-toast-success";

        // ─── Données ─────────────────────────────────────────────
        private DashboardITStatsDto? _stats = null;

        // ─── Filtre : Évolution des incidents (semaines) ─────────
        private DateTime _semDebut = DateTime.Today.AddDays(-55);
        private DateTime _semFin   = DateTime.Today;
        private HubConnection? _hubConnection;

        // ─── Heatmap ─────────────────────────────────────────────
        private int    _heatmapAnnee    = DateTime.Today.Year;
        private int _heatmapMaterielId = 0; 
        private List<int> _heatmapAnnees = new();

        protected override async Task OnInitializedAsync()
        {
            VoiceSvc.OnCommand -= HandleVoiceCommand; 
            try
            {
                var savedTheme = await JS.InvokeAsync<string?>("eval",
                    "localStorage.getItem('theme')");
                _theme = savedTheme == "light" ? "light" : "dark";
            }
            catch { }

            await ChargerInfosUtilisateur();
            await ChargerDonnees();
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

            _hubConnection.On("DashboardITUpdated", async () =>
            {
                await MettreAJourSilencieusement();
            });

            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinDashboardIT");
            }
            catch { /* dashboard reste statique si SignalR non dispo */ }
        }
        private async Task MettreAJourSilencieusement()
        {
            var nouvelles = await StatSvc.GetDashboardAsync();
            if (nouvelles == null) return;

            bool kpiChanged      = KpisOntChange(nouvelles);
            bool incidentChanged = IncidentsOntChange(nouvelles);
            bool articleChanged  = ArticlesOntChange(nouvelles);
            bool affectChanged   = AffectationsOntChange(nouvelles);

            _stats = nouvelles;
            _lastUpdate = DateTime.Now.ToString("HH:mm");

            await InvokeAsync(async () =>
            {
                bool dark = _theme == "dark";

                if (kpiChanged)
                    StateHasChanged();

                if (incidentChanged)
                {
                    await JS.InvokeVoidAsync("ApexITInterop.renderIncidentsParType",
                        "chart-incidents-type", _stats.IncidentsParType, dark);
                    await JS.InvokeVoidAsync("ApexITInterop.renderIncidentStatutGauge",
                        "chart-incidents-statut", _stats.IncidentParStatut, dark);
                    await RenderEvolutionIncidents(dark);
                    await JS.InvokeVoidAsync("ApexITInterop.renderTendanceResolution",
                        "chart-tendance-resolution", _stats.TendanceResolution, dark);
                }

                if (articleChanged)
                    await JS.InvokeVoidAsync("ApexITInterop.renderArticlesParStatut",
                        "chart-articles-statut", _stats.ArticlesParStatut, dark);

                if (affectChanged)
                {
                    await JS.InvokeVoidAsync("ApexITInterop.renderAffectationsParDept",
                        "chart-affectations-dept", _stats.AffectationsParDept, dark);
                    await JS.InvokeVoidAsync("ApexITInterop.renderEquipementsParCategorie",
                        "chart-equipements-categorie", _stats.EquipementsParCategorie, dark);
                }
            });
        }
        private bool KpisOntChange(DashboardITStatsDto n) =>
        _stats == null ||
        _stats.TotalMateriels         != n.TotalMateriels         ||
        _stats.TotalArticles          != n.TotalArticles          ||
        _stats.IncidentsActifs        != n.IncidentsActifs        ||
        _stats.AffectationsEnCours    != n.AffectationsEnCours    ||
        _stats.ArticlesHorsService    != n.ArticlesHorsService    ||
        _stats.DemandesAchatEnAttente != n.DemandesAchatEnAttente;

    private bool IncidentsOntChange(DashboardITStatsDto n) =>
        _stats == null ||
        _stats.IncidentsRaw.Count           != n.IncidentsRaw.Count           ||
        _stats.IncidentParStatut.EnAttente  != n.IncidentParStatut.EnAttente  ||
        _stats.IncidentParStatut.EnCours    != n.IncidentParStatut.EnCours    ||
        _stats.IncidentParStatut.Resolu     != n.IncidentParStatut.Resolu     ||
        _stats.IncidentParStatut.Cloture    != n.IncidentParStatut.Cloture;

    private bool ArticlesOntChange(DashboardITStatsDto n) =>
        _stats == null ||
        _stats.ArticlesParStatut.Disponible   != n.ArticlesParStatut.Disponible   ||
        _stats.ArticlesParStatut.Affecte      != n.ArticlesParStatut.Affecte      ||
        _stats.ArticlesParStatut.HorsService  != n.ArticlesParStatut.HorsService  ||
        _stats.ArticlesParStatut.EnReparation != n.ArticlesParStatut.EnReparation;

    private bool AffectationsOntChange(DashboardITStatsDto n) =>
        _stats == null ||
        _stats.AffectationsEnCours              != n.AffectationsEnCours ||
        _stats.AffectationsParDept.Count        != n.AffectationsParDept.Count ||
        _stats.EquipementsParCategorie.Count    != n.EquipementsParCategorie.Count;
        private async Task HandleVoiceCommand(VoiceCommand cmd)
        {
            if (cmd.Type == VoiceCommandType.Navigation)
            {
            }
            // D'autres commandes spécifiques à cette page peuvent être ajoutées ici
            await InvokeAsync(StateHasChanged);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            try
            {
                await JS.InvokeVoidAsync("eval", @"
                    window.__assetflowSetThemeIT = function(ref) {
                        if (window.__themeObsIT) window.__themeObsIT.disconnect();
                        window.__themeObsIT = new MutationObserver(function() {
                            var dark = document.documentElement.classList.contains('dark');
                            ref.invokeMethodAsync('OnThemeChanged', dark);
                        });
                        window.__themeObsIT.observe(document.documentElement,
                            { attributes: true, attributeFilter: ['class'] });
                    };
                ");
                var dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("__assetflowSetThemeIT", dotNetRef);
            }
            catch { }

            if (_stats != null)
                await RenderAllCharts();
        }

        [JSInvokable("OnThemeChanged")]
        public async Task OnThemeChanged(bool isDark)
        {
            _theme = isDark ? "dark" : "light";
            await InvokeAsync(StateHasChanged);
            if (_stats != null)
                await RenderAllCharts();
        }

        // ─── Chargement ──────────────────────────────────────────

        private async Task ChargerDonnees()
        {
            _chargement = true;
            StateHasChanged();

            _stats = await StatSvc.GetDashboardAsync();
            _lastUpdate = DateTime.Now.ToString("HH:mm");

            _chargement = false;
            StateHasChanged();

            await Task.Delay(80); // DOM settle
            if (_stats != null)
            {
                await RenderAllCharts();
                var minYear = _stats.IncidentsRaw.Any()
                    ? _stats.IncidentsRaw.Min(i => i.DateIncident.Year)
                    : DateTime.Today.Year;
                _heatmapAnnees = Enumerable.Range(minYear, DateTime.Today.Year - minYear + 1).ToList();
                if (!_heatmapAnnees.Any()) _heatmapAnnees.Add(DateTime.Today.Year);
            }
        }

        private async Task ChargerInfosUtilisateur()
        {
            try
            {
                var nom = await JS.InvokeAsync<string?>("eval",
                    "localStorage.getItem('user_name') || localStorage.getItem('userFullName') || localStorage.getItem('currentUserName')");
                var role = await JS.InvokeAsync<string?>("eval",
                    "localStorage.getItem('user_role') || localStorage.getItem('currentUserRole')");

                if (!string.IsNullOrWhiteSpace(nom))
                {
                    _nomUtilisateur = Nettoyer(nom);
                    var parts = _nomUtilisateur.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    _initiales = parts.Length >= 2
                        ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                        : _nomUtilisateur[..Math.Min(2, _nomUtilisateur.Length)].ToUpper();
                }
                if (!string.IsNullOrWhiteSpace(role))
                    _roleUtilisateur = Nettoyer(role);
            }
            catch { }
        }

        // ─── Rendu de tous les graphes ────────────────────────────

        private async Task RenderAllCharts()
        {
            if (_stats == null) return;
            bool dark = _theme == "dark";

            try
            {
                // 1. Incidents par type (Donut)
                await JS.InvokeVoidAsync("ApexITInterop.renderIncidentsParType",
                    "chart-incidents-type", _stats.IncidentsParType, dark);

                // 2. Statut des incidents (Donut)
                await JS.InvokeVoidAsync("ApexITInterop.renderIncidentStatutGauge",
                    "chart-incidents-statut", _stats.IncidentParStatut, dark);

                // 3. Évolution incidents par semaine (Area stacked)
                await RenderEvolutionIncidents(dark);

                // 4. Articles par statut (Bar horizontal stacked 100%)
                await JS.InvokeVoidAsync("ApexITInterop.renderArticlesParStatut",
                    "chart-articles-statut", _stats.ArticlesParStatut, dark);

                // 5. Affectations par département (Bar vertical distribué)
                await JS.InvokeVoidAsync("ApexITInterop.renderAffectationsParDept",
                    "chart-affectations-dept", _stats.AffectationsParDept, dark);

                // 6. Tendance résolution (Line)
                await JS.InvokeVoidAsync("ApexITInterop.renderTendanceResolution",
                    "chart-tendance-resolution", _stats.TendanceResolution, dark);

                // 7. Équipements par catégorie (Radial Bar)
                await JS.InvokeVoidAsync("ApexITInterop.renderEquipementsParCategorie",
                    "chart-equipements-categorie", _stats.EquipementsParCategorie, dark);
                // 8. Heatmap
                await Task.Delay(50);
                await RenderHeatmap();
            }
            catch (Exception ex)
            {
                AfficherToast($"Erreur graphes : {ex.Message}", "it-toast-error");
            }
        }
        private void OnHeatmapAnneeChange(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var y)) _heatmapAnnee = y;
            _ = RenderHeatmap();
        }

        private void OnHeatmapMaterielChange(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var id))
                _heatmapMaterielId = id;
            _ = RenderHeatmap();
        }

        private async Task RenderHeatmap()
        {
            if (_stats == null) return;

            var incidents = _stats.IncidentsRaw
                .Where(i => i.DateIncident.Year == _heatmapAnnee);

            if (_heatmapMaterielId != 0)
                incidents = incidents.Where(i => i.MaterielId == _heatmapMaterielId);

            var data = incidents
                .GroupBy(i => i.DateIncident.Date.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => g.Count());

            await JS.InvokeVoidAsync("ApexITInterop.renderHeatmap",
                "heatmap-grid", "heatmap-months-labels", data, _heatmapAnnee, _theme == "dark");
        }

        // ─── Rendu individuel — Évolution ────────────────────────

        private async Task RenderEvolutionIncidents(bool dark)
        {
            if (_stats == null) return;
            var semaines = _stats.GetIncidentsParSemaine(_semDebut, _semFin, 8);
            await JS.InvokeVoidAsync("ApexITInterop.renderEvolutionIncidents",
                "chart-incidents-evolution", semaines, dark);
        }

        // ─── Handlers filtres ─────────────────────────────────────

        private void OnSemDebutChange(ChangeEventArgs e)
        { if (DateTime.TryParse(e.Value?.ToString(), out var d)) _semDebut = d; }

        private void OnSemFinChange(ChangeEventArgs e)
        { if (DateTime.TryParse(e.Value?.ToString(), out var d)) _semFin = d; }

        private async Task AppliquerSemaines()
        {
            if (_stats != null) await RenderEvolutionIncidents(_theme == "dark");
        }

        private void ToggleSidebar() => _sidebarOpen = !_sidebarOpen;

        private static string Nettoyer(string v)
        {
            v = v.Trim();
            if (v.Length >= 2 &&
                ((v.StartsWith('"') && v.EndsWith('"')) ||
                 (v.StartsWith('\'') && v.EndsWith('\''))))
                v = v[1..^1].Trim();
            return v;
        }

        private async void AfficherToast(string msg, string type)
        {
            _toastMsg  = msg;
            _toastType = type;
            StateHasChanged();
            await Task.Delay(3500);
            _toastMsg = string.Empty;
            StateHasChanged();
        }

        public async ValueTask DisposeAsync()
        {
            VoiceSvc.OnCommand -= HandleVoiceCommand;

            if (_hubConnection is not null)
            {
                try { await _hubConnection.InvokeAsync("LeaveDashboardIT"); } catch { }
                await _hubConnection.DisposeAsync();
            }

            try { await JS.InvokeVoidAsync("ApexITInterop.destroyAll"); } catch { }
        }
            }
}
