using AssetFlow.Application.DTOs;
using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.SignalR.Client;
namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class Statistiques : ComponentBase, IAsyncDisposable
    {
        [Inject] private IJSRuntime          JS      { get; set; } = default!;
        [Inject] private StatistiquesService StatSvc { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;
        [Inject] private HttpClient Http { get; set; } = default!;
        // ─── UI ──────────────────────────────────────────────────
        private string _theme           = "dark";
        private bool   _sidebarOpen     = false;
        private string _nomUtilisateur  = "Agent Achat";
        private string _initiales       = "AA";
        private bool   _chargement      = true;

        private string _toastMsg  = string.Empty;
        private string _toastType = "toast-success";

        // ─── Données brutes (toutes périodes) ────────────────────
        private DashboardStatsDto? _stats = null;

        // ─── Options de filtres ──────────────────────────────────
        private List<int> _annees = Enumerable.Range(DateTime.Now.Year - 4, 6).Reverse().ToList();
        private record MoisOption(int Value, string Label);
        private List<MoisOption> _moisOptions = new()
        {
            new(1,"Janvier"), new(2,"Février"),  new(3,"Mars"),
            new(4,"Avril"),   new(5,"Mai"),       new(6,"Juin"),
            new(7,"Juillet"), new(8,"Août"),      new(9,"Septembre"),
            new(10,"Octobre"),new(11,"Novembre"), new(12,"Décembre"),
        };

        // ─── Filtre : Articles par catégorie ─────────────────────
        private string _filtreArticlesCat = "all";

        // ─── Filtre : État des demandes ───────────────────────────
        private int _etatAnnee = DateTime.Now.Year;
        private int _etatMois  = 0;  // 0 = tous les mois

        // ─── Filtre : Demandes par semaine ────────────────────────
        // 8 semaines avant aujourd'hui par défaut
        private DateTime _semDebut = DateTime.Today.AddDays(-55);
        private DateTime _semFin   = DateTime.Today;

        // ─── Filtre : Articles par matériel ──────────────────────
        private string _filtreArticlesMat = "all";

        // ─── Filtre : Demandes par mois ───────────────────────────
        private int _moisDemandes  = DateTime.Now.Month;
        private int _anneeDemandes = DateTime.Now.Year;
        private string      _roleUtilisateur = "Service Achat";
        private bool _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        private HubConnection? _hubConnection;

        protected override async Task OnInitializedAsync()
        {
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
            var hubUrl = Http.BaseAddress!.ToString().TrimEnd('/') + "/dashboardhub";
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
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

            // Quand n'importe quelle donnée change → recharger tout
            _hubConnection.On("DashboardUpdated", async () =>
            {
                await MettreAJourSilencieusement();
            });

            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinDashboard");
            }
            catch { /* SignalR non dispo, dashboard reste statique */ }
        }
        private async Task MettreAJourSilencieusement()
        {
            // 1. Récupérer les nouvelles données en arrière-plan
            var nouvellesStats = await StatSvc.GetDashboardAsync(DateTime.Now.Year, 1, 12);
            if (nouvellesStats == null) return;

            // 2. Comparer et mettre à jour uniquement ce qui a changé
            bool kpiChanged     = KpisOntChange(nouvellesStats);
            bool demandeChanged = DemandesOntChange(nouvellesStats);
            bool materielChanged= MaterielsOntChange(nouvellesStats);
            bool articleChanged = ArticlesOntChange(nouvellesStats);

            // 3. Mettre à jour les données en mémoire
            _stats = nouvellesStats;

            // 4. Re-render uniquement les graphes impactés (sans flash global)
            await InvokeAsync(async () =>
            {
                bool dark = _theme == "dark";

                if (kpiChanged)
                    StateHasChanged(); // juste les KPIs HTML, pas de graphe

                if (demandeChanged)
                {
                    await RenderEtatDemandes(dark);
                    await RenderDemandesSemaine(dark);
                    await RenderDemandesMois(dark);
                }

                if (materielChanged)
                    await JS.InvokeVoidAsync("ApexInterop.renderAffectationMateriel",
                        "chart-affectation", _stats.AffectationMateriel, dark);

                if (articleChanged)
                {
                    await RenderArticlesCategorie(dark);
                    await RenderArticlesMateriel(dark);
                }
            });
        }
        private bool KpisOntChange(DashboardStatsDto nouvelles) =>
            _stats == null ||
            _stats.TotalMateriels       != nouvelles.TotalMateriels       ||
            _stats.TotalCommandes       != nouvelles.TotalCommandes       ||
            _stats.TotalArticles        != nouvelles.TotalArticles        ||
            _stats.TotalDemandesActives != nouvelles.TotalDemandesActives;

        private bool DemandesOntChange(DashboardStatsDto nouvelles) =>
            _stats == null ||
            _stats.DemandesRaw.Count != nouvelles.DemandesRaw.Count ||
            _stats.DemandesRaw.Any(d =>
                !nouvelles.DemandesRaw.Any(n =>
                    n.DateCreation == d.DateCreation && n.Statut == d.Statut));

        private bool MaterielsOntChange(DashboardStatsDto nouvelles) =>
            _stats == null ||
            _stats.AffectationMateriel.Affecte    != nouvelles.AffectationMateriel.Affecte ||
            _stats.AffectationMateriel.NonAffecte != nouvelles.AffectationMateriel.NonAffecte;

        private bool ArticlesOntChange(DashboardStatsDto nouvelles) =>
            _stats == null ||
            _stats.TotalArticles != nouvelles.TotalArticles ||
            _stats.ArticlesParCategorie.Count != nouvelles.ArticlesParCategorie.Count ||
            _stats.ArticlesParMateriel.Count  != nouvelles.ArticlesParMateriel.Count;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            try
            {
                await JS.InvokeVoidAsync("eval", @"
                    window.__assetflowSetThemeStats = function(ref) {
                        if (window.__themeObsStats) window.__themeObsStats.disconnect();
                        window.__themeObsStats = new MutationObserver(function() {
                            var dark = document.documentElement.classList.contains('dark');
                            ref.invokeMethodAsync('OnThemeChanged', dark);
                        });
                        window.__themeObsStats.observe(document.documentElement,
                            { attributes: true, attributeFilter: ['class'] });
                    };
                ");
                var dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("__assetflowSetThemeStats", dotNetRef);
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

        // ─── Chargement global (données brutes) ──────────────────

        private async Task ChargerDonnees()
        {
            _chargement = true;
            StateHasChanged();

            // Charger toutes les données en une seule requête (sans filtre global)
            _stats = await StatSvc.GetDashboardAsync(DateTime.Now.Year, 1, 12);

            _chargement = false;
            StateHasChanged();

            await Task.Delay(80); // DOM settle
            if (_stats != null)
                await RenderAllCharts();
        }

        private async Task ChargerInfosUtilisateur()
        {
            try
            {
                var nom  = await JS.InvokeAsync<string?>("eval",
                    "localStorage.getItem('user_name') || localStorage.getItem('userFullName') || localStorage.getItem('currentUserName')");
                var role = await LocalStorage.GetItemAsync<string>("user_role");

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
                // 1. Articles par catégorie (filtrés côté client)
                await RenderArticlesCategorie(dark);

                // 2. État des demandes (filtré côté client)
                await RenderEtatDemandes(dark);

                // 3. Demandes par semaine sur période choisie
                await RenderDemandesSemaine(dark);

                // 4. Affectation matériel
                await JS.InvokeVoidAsync("ApexInterop.renderAffectationMateriel",
                    "chart-affectation", _stats.AffectationMateriel, dark);

                // 5. Articles par matériel (filtrés côté client)
                await RenderArticlesMateriel(dark);

                // 6. Demandes par semaine du mois sélectionné
                await RenderDemandesMois(dark);
            }
            catch (Exception ex)
            {
                AfficherToast($"Erreur graphes : {ex.Message}", "toast-error");
            }
        }

        // ─── Rendus individuels ───────────────────────────────────

        private async Task RenderArticlesCategorie(bool dark)
        {
            if (_stats == null) return;
            // Agréger ArticlesParMateriel par catégorie
            var parCategorie = _stats.ArticlesParCategorie;
            await JS.InvokeVoidAsync("ApexInterop.renderArticlesParCategorie",
                "chart-articles-categorie", parCategorie, dark, _filtreArticlesCat);
        }

        private async Task RenderEtatDemandes(bool dark)
        {
            if (_stats == null) return;
            // Filtrer par année et éventuellement par mois côté client
            var etat = _stats.GetEtatDemandes(_etatAnnee, _etatMois);
            await JS.InvokeVoidAsync("ApexInterop.renderEtatDemandes",
                "chart-etat-demandes", etat, dark);
        }

        private async Task RenderDemandesSemaine(bool dark)
        {
            if (_stats == null) return;
            // Calculer 8 semaines à partir de la plage choisie
            var semaines = _stats.GetDemandesParSemaine(_semDebut, _semFin, 8);
            await JS.InvokeVoidAsync("ApexInterop.renderDemandesParSemaine",
                "chart-demandes-semaine", semaines, dark);
        }

        private async Task RenderArticlesMateriel(bool dark)
        {
            if (_stats == null) return;
            await JS.InvokeVoidAsync("ApexInterop.renderArticlesParMateriel",
                "chart-articles-materiel", _stats.ArticlesParMateriel, dark, _filtreArticlesMat);
        }

        private async Task RenderDemandesMois(bool dark)
        {
            if (_stats == null) return;
            var semaines = _stats.GetDemandesSemaineDuMois(_anneeDemandes, _moisDemandes);
            await JS.InvokeVoidAsync("ApexInterop.renderDemandesSemaineDuMois",
                "chart-demandes-mois", semaines, dark);
        }

        // ─── Handlers filtres ─────────────────────────────────────

        // Graphe 1 — Articles catégorie
        private async Task SetFiltreArticlesCat(string f)
        {
            _filtreArticlesCat = f;
            if (_stats != null) await RenderArticlesCategorie(_theme == "dark");
        }

        // Graphe 2 — État demandes
        private void OnEtatAnneeChange(ChangeEventArgs e)
        { if (int.TryParse(e.Value?.ToString(), out var v)) _etatAnnee = v; }
        private async void OnEtatMoisChange(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var v)) _etatMois = v;
            if (_stats != null) await RenderEtatDemandes(_theme == "dark");
        }

        // Graphe 3 — Semaines
        private void OnSemDebutChange(ChangeEventArgs e)
        { if (DateTime.TryParse(e.Value?.ToString(), out var d)) _semDebut = d; }
        private void OnSemFinChange(ChangeEventArgs e)
        { if (DateTime.TryParse(e.Value?.ToString(), out var d)) _semFin = d; }
        private async Task AppliquerSemaines()
        {
            if (_stats != null) await RenderDemandesSemaine(_theme == "dark");
        }

        // Graphe 5 — Articles matériel
        private async Task SetFiltreArticlesMat(string f)
        {
            _filtreArticlesMat = f;
            if (_stats != null) await RenderArticlesMateriel(_theme == "dark");
        }

        // Graphe 6 — Demandes du mois
        private void OnMoisDemandesChange(ChangeEventArgs e)
        { if (int.TryParse(e.Value?.ToString(), out var v)) _moisDemandes = v; }
        private void OnAnneeDemandesChange(ChangeEventArgs e)
        { if (int.TryParse(e.Value?.ToString(), out var v)) _anneeDemandes = v; }
        private async Task AppliquerDemandesMois()
        {
            if (_stats != null) await RenderDemandesMois(_theme == "dark");
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
            if (_hubConnection is not null)
            {
                try { await _hubConnection.InvokeAsync("LeaveDashboard"); } catch { }
                await _hubConnection.DisposeAsync();
            }
            try { await JS.InvokeVoidAsync("ApexInterop.destroyAll"); }
            catch { }
        }
    }
}
