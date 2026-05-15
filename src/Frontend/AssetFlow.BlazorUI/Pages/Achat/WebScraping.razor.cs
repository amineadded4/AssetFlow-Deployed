using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;
using AssetFlow.BlazorUI.DTOs;
using AssetFlow.BlazorUI.Services;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class WebScraping : ComponentBase, IAsyncDisposable
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IHttpClientFactory HttpFactory { get; set; } = default!;
        [Inject] private ScraperCircuitBreakerService _circuitBreaker { get; set; } = default!;
        [Inject] private ScrapingBackgroundService ScrapingBg { get; set; } = default!;
        [Inject] private Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; } = default!;

        // ── Countdown circuit breaker
        private int _countdownSecondes = 0;
        private System.Threading.Timer? _countdownTimer;

        // ── État
        private string _theme = "dark";
        private bool _sidebarOpen = false;
        private string _nomUtilisateur = "Agent Achat";
        private string _initiales = "AA";
        private string _roleUtilisateur = "Service Achat";
        private bool _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        private string _recherche = string.Empty;
        private string? _nomRecherche = null;
        private string? _derniereRecherche = null;
        private bool _chargement = false;

        private List<ResultatScraping> _resultats = new();
        private string _filtreActif = "prix";

        // ── Filtres avancés
        private HashSet<string> _filtresSites = new();
        private decimal _prixMin = 0;
        private decimal _prixMax = 9999;
        private decimal _prixAbsoluMin = 0;
        private decimal _prixAbsoluMax = 9999;

        // Toast
        private string _toastMsg = string.Empty;
        private string _toastType = "ws-toast-success";

        // ── Filtre disponibilité
        private string? _filtreDisponibilite = null;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var isDark = await JS.InvokeAsync<bool>("eval",
                    "document.documentElement.classList.contains('dark')");
                _theme = isDark ? "dark" : "light";
            }
            catch { }

            await ChargerInfosUtilisateur();
            LireQueryString();
            DemarrerCountdown();

            // ── Init SignalR scraping background
            var token = await LocalStorage.GetItemAsync<string>("access_token") ?? "";
            var userId = await LocalStorage.GetItemAsync<int>("user_id");
            await ScrapingBg.InitAsync(token, userId.ToString());

            // ── S'abonner aux résultats
            ScrapingBg.OnTermine += OnScrapingTermine;
            ScrapingBg.OnChanged += OnScrapingChanged;

            // ── Demander permission notification navigateur
            try
            {
                await JS.InvokeAsync<string>("requestNotificationPermission");
            }
            catch { }

            // ── Si résultat en cache Redis, le charger
            await ChargerDepuisCache(userId.ToString());
        }

        // ── Charger le cache Redis au démarrage
        private async Task ChargerDepuisCache(string userId)
        {
            try
            {
                var http     = HttpFactory.CreateClient("ApiClient");
                var response = await http.GetAsync($"api/scraping/cache?userId={userId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    AppliquerResultats(json, "(cache)");
                }
            }
            catch { }
        }

        // ── Callback quand scraping termine
        private void OnScrapingTermine(ScrapingResultatDto res)
        {
            InvokeAsync(() =>
            {
                _chargement = false;

                if (res.Succes && !string.IsNullOrEmpty(res.JsonResultat))
                {
                    _circuitBreaker.EnregistrerSucces(); // ← succès
                    AppliquerResultats(res.JsonResultat, res.Query);
                    AfficherToast($"{res.NombreResultats} résultat(s) trouvé(s)", "ws-toast-success");
                }
                else
                {
                    _circuitBreaker.EnregistrerEchec(); // ← échec → circuit breaker
                    _resultats = new();
                    // Remplacer le message technique par le message circuit breaker
                    var msg = _circuitBreaker.Etat == CircuitState.Open
                        ? _circuitBreaker.MessageUtilisateur
                        : "Le service de recherche est momentanément indisponible. Veuillez réessayer.";
                    AfficherToast(msg, "ws-toast-error");
                }

                StateHasChanged();
            });
        }

        private void OnScrapingChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        // ── Parser et appliquer les résultats JSON
        private void AppliquerResultats(string json, string query)
        {
            try
            {
                var reponse = JsonSerializer.Deserialize<ReponseScraping>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (reponse?.resultats != null && reponse.resultats.Any())
                {
                    _nomRecherche = query;
                    _derniereRecherche = query;
                    _resultats = reponse.resultats.Select(r => new ResultatScraping
                    {
                        Site = r.site,
                        NomProduit = r.nom_produit,
                        Prix = r.prix,
                        EnStock = r.stock?.Contains("stock", StringComparison.OrdinalIgnoreCase) == true,
                        Livraison = "Non précisé",
                        Garantie = "Non précisée",
                        Url = r.url
                    }).ToList();

                    _prixAbsoluMin = _resultats.Min(r => r.Prix);
                    _prixAbsoluMax = _resultats.Max(r => r.Prix);
                    _prixMin = _prixAbsoluMin;
                    _prixMax = _prixAbsoluMax;
                    _filtresSites = new();
                }
            }
            catch { }
        }

        // ── Lancement recherche — Fire & Forget via background service
        private async Task LancerRecherche()
        {
            if (string.IsNullOrWhiteSpace(_recherche)) return;

            // Circuit breaker
            if (_circuitBreaker.VerifierTransitionHalfOpen())
            {
                StateHasChanged();
                await Task.Delay(4200);
            }

            if (!_circuitBreaker.PeutEnvoyerRequete())
            {
                AfficherToast(_circuitBreaker.MessageUtilisateur, "ws-toast-error");
                return;
            }

            _nomRecherche = _recherche.Trim();
            _derniereRecherche = _nomRecherche;
            _chargement = true;
            _resultats = new();
            _filtresSites = new();
            _filtreActif = "prix";
            StateHasChanged();

            try
            {
                await ScrapingBg.LancerAsync(_nomRecherche);
            }
            catch (HttpRequestException ex) when ((int?)ex.StatusCode >= 500 || (int?)ex.StatusCode >= 400 || ex.StatusCode == null)
            {
                _chargement = false;
                _circuitBreaker.EnregistrerEchec();
                var msg = _circuitBreaker.Etat == CircuitState.Open
                    ? _circuitBreaker.MessageUtilisateur
                    : "Le service de recherche est momentanément indisponible. Veuillez réessayer.";
                AfficherToast(msg, "ws-toast-error");
                StateHasChanged();
            }
            catch (Exception)
            {
                _chargement = false;
                _circuitBreaker.EnregistrerEchec();
                AfficherToast("Le service de recherche est momentanément indisponible. Veuillez réessayer.", "ws-toast-error");
                StateHasChanged();
            }
        }

        // ── Countdown circuit breaker
        private void DemarrerCountdown()
        {
            _countdownTimer = new System.Threading.Timer(async _ =>
            {
                _countdownSecondes = _circuitBreaker.SecondesRestantes;
                await InvokeAsync(StateHasChanged);
            }, null, 0, 1000);
        }

        public async ValueTask DisposeAsync()
        {
            ScrapingBg.OnTermine -= OnScrapingTermine;
            ScrapingBg.OnChanged -= OnScrapingChanged;

            if (_countdownTimer != null)
                await _countdownTimer.DisposeAsync();
        }

        // ── Query string
        private void LireQueryString()
        {
            var uri = new Uri(Nav.Uri);
            var query = uri.Query.TrimStart('?');
            foreach (var param in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = param.Split('=', 2);
                if (parts.Length == 2 &&
                    Uri.UnescapeDataString(parts[0]) == "q" &&
                    !string.IsNullOrWhiteSpace(parts[1]))
                {
                    _recherche = Uri.UnescapeDataString(parts[1]).Trim();
                    break;
                }
            }
        }

        // ── Utilisateur
        private async Task ChargerInfosUtilisateur()
        {
            try
            {
                var nom = await JS.InvokeAsync<string?>("eval",
                    "localStorage.getItem('user_name') || " +
                    "localStorage.getItem('userFullName') || " +
                    "localStorage.getItem('currentUserName')");

                var role = await JS.InvokeAsync<string?>("eval",
                    "localStorage.getItem('user_role') || " +
                    "localStorage.getItem('currentUserRole')");

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

        // ── Actions UI
        private void ToggleSidebar() => _sidebarOpen = !_sidebarOpen;

        private void ViderRecherche()
        {
            _recherche = string.Empty;
            _nomRecherche = null;
            _derniereRecherche = null;
            _resultats = new();
            _filtresSites = new();
        }

        private async Task OnKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(_recherche))
                await LancerRecherche();
        }

        // ── Filtres avancés
        private void ToggleSite(string site)
        {
            if (_filtresSites.Contains(site)) _filtresSites.Remove(site);
            else _filtresSites.Add(site);
        }

        private void OnPrixMinChange(ChangeEventArgs e)
        {
            if (decimal.TryParse(e.Value?.ToString(), out var val))
                _prixMin = Math.Min(val, _prixMax - 1);
        }

        private void OnPrixMaxChange(ChangeEventArgs e)
        {
            if (decimal.TryParse(e.Value?.ToString(), out var val))
                _prixMax = Math.Max(val, _prixMin + 1);
        }

        private void ResetFiltres()
        {
            _filtresSites = new();
            _prixMin = _prixAbsoluMin;
            _prixMax = _prixAbsoluMax;
        }

        // ── Export CSV
        private async Task ExporterCsv()
        {
            if (!_resultats.Any()) return;
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Site;Produit;Prix (DT);Disponibilité;Lien");
                foreach (var r in _resultats)
                    sb.AppendLine($"{r.Site};{r.NomProduit.Replace(";", ",")};{r.Prix:N3};{(r.EnStock ? "En stock" : "Rupture")};{r.Url}");

                var bytes = System.Text.Encoding.UTF8.GetPreamble()
                    .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                var base64 = Convert.ToBase64String(bytes);
                var nom = $"scraping-{_nomRecherche?.Replace(" ", "-")}-{DateTime.Now:yyyyMMdd}.csv";

                await JS.InvokeVoidAsync("eval", $@"
                    (function(){{
                        var a = document.createElement('a');
                        a.href = 'data:text/csv;base64,{base64}';
                        a.download = '{nom}';
                        document.body.appendChild(a);
                        a.click();
                        document.body.removeChild(a);
                    }})();
                ");
                AfficherToast("Export CSV téléchargé.", "ws-toast-success");
            }
            catch (Exception ex)
            {
                AfficherToast($"Erreur export : {ex.Message}", "ws-toast-error");
            }
        }

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
            _toastMsg = msg;
            _toastType = type;
            StateHasChanged();
            await Task.Delay(3500);
            _toastMsg = string.Empty;
            StateHasChanged();
        }

        // ── Méthodes JS (theme)
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;
            try
            {
                await JS.InvokeVoidAsync("eval", @"
                    window.__wsThemeRef = null;
                    window.__wsSetTheme = function(ref) {
                        window.__wsThemeRef = ref;
                        if (window.__wsThemeObs) window.__wsThemeObs.disconnect();
                        window.__wsThemeObs = new MutationObserver(function() {
                            var dark = document.documentElement.classList.contains('dark');
                            window.__wsThemeRef &&
                                window.__wsThemeRef.invokeMethodAsync('OnThemeChanged', dark);
                        });
                        window.__wsThemeObs.observe(document.documentElement, {
                            attributes: true, attributeFilter: ['class']
                        });
                    };
                ");
                var dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("__wsSetTheme", dotNetRef);
            }
            catch { }

            if (!string.IsNullOrWhiteSpace(_recherche))
                await LancerRecherche();
        }

        [JSInvokable("OnThemeChanged")]
        public void OnThemeChanged(bool isDark)
        {
            _theme = isDark ? "dark" : "light";
            InvokeAsync(StateHasChanged);
        }
    }
}