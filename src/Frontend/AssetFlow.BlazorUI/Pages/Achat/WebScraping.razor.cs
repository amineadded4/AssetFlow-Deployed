// ============================================================
// FICHIER  : Pages/Achat/WebScraping.razor.cs
// ============================================================

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http;
using System.Net.Http.Json;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class WebScraping : ComponentBase
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        
        [Inject] private IHttpClientFactory HttpFactory { get; set; } = default!;

        private class ResultatScraping
        {
            public string Site { get; set; } = string.Empty;
            public string NomProduit { get; set; } = string.Empty;
            public decimal Prix { get; set; }
            public bool EnStock { get; set; }
            public string Livraison { get; set; } = "Non précisé";
            public string Garantie { get; set; } = "Non précisée";
            public string Url { get; set; } = string.Empty;
        }

        private class ReponseScraping
        {
            public bool succes { get; set; }
            public string article { get; set; } = string.Empty;
            public string date_recherche { get; set; } = string.Empty;
            public int nombre_resultats { get; set; }
            public List<ResultatPython> resultats { get; set; } = new();
            public MeilleurPrix? meilleur_prix { get; set; }
            public Recommandation? recommandation { get; set; }
        }

        private class ResultatPython
        {
            public string site { get; set; } = string.Empty;
            public string nom_produit { get; set; } = string.Empty;
            public decimal prix { get; set; }
            public string devise { get; set; } = string.Empty;
            public string stock { get; set; } = string.Empty;
            public string url { get; set; } = string.Empty;
            public string date_scraping { get; set; } = string.Empty;
        }

        private class MeilleurPrix
        {
            public string site { get; set; } = string.Empty;
            public string nom_produit { get; set; } = string.Empty;
            public decimal prix { get; set; }
            public string stock { get; set; } = string.Empty;
            public string url { get; set; } = string.Empty;
        }

        private class Recommandation
        {
            public string? site { get; set; }
            public decimal? prix { get; set; }
            public string? url { get; set; }
            public string message { get; set; } = string.Empty;
        }

        // ── État ────────────────────────────────────────────────
        private string _theme = "dark";
        private bool _sidebarOpen = false;
        private string _nomUtilisateur = "Adem Added";
        private string _initiales = "AA";

        private string _recherche = string.Empty;
        private string? _nomRecherche = null;
        private string? _derniereRecherche = null;
        private bool _chargement = false;

        private List<ResultatScraping> _resultats = new();
        private string _filtreActif = "prix";

        // ── Filtres avancés ─────────────────────────────────────
        private HashSet<string> _filtresSites = new();
        private decimal _prixMin = 0;
        private decimal _prixMax = 9999;
        private decimal _prixAbsoluMin = 0;
        private decimal _prixAbsoluMax = 9999;

        // Toast
        private string _toastMsg = string.Empty;
        private string _toastType = "ws-toast-success";
        private string      _roleUtilisateur = "Service Achat";
        private bool _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        // ── Lifecycle ───────────────────────────────────────────

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
        }

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

        // ── Query string ────────────────────────────────────────

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

        // ── Utilisateur ─────────────────────────────────────────

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

        // ── Actions ─────────────────────────────────────────────

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

        // ── Filtres avancés ─────────────────────────────────────

        private void ToggleSite(string site)
        {
            if (_filtresSites.Contains(site))
                _filtresSites.Remove(site);
            else
                _filtresSites.Add(site);
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

        // ── Recherche principale ────────────────────────────────

        private async Task LancerRecherche()
        {
            if (string.IsNullOrWhiteSpace(_recherche)) return;

            _nomRecherche = _recherche.Trim();
            _derniereRecherche = _nomRecherche;
            _chargement = true;
            _resultats = new();
            _filtresSites = new();
            _filtreActif = "prix";
            StateHasChanged();

            try
            {
                var http = HttpFactory.CreateClient("PythonScraper");
                var reponse = await http.GetFromJsonAsync<ReponseScraping>(
                    $"scrape?q={Uri.EscapeDataString(_nomRecherche)}"
                );

                if (reponse?.succes == true && reponse.resultats != null && reponse.resultats.Any())
                {
                    _resultats = reponse.resultats.Select(r => new ResultatScraping
                    {
                        Site = r.site,
                        NomProduit = r.nom_produit,
                        Prix = r.prix,
                        EnStock = r.stock?.Contains("stock", StringComparison.OrdinalIgnoreCase) == true ||
                                  r.stock?.Contains("En stock", StringComparison.OrdinalIgnoreCase) == true,
                        Livraison = "Non précisé",
                        Garantie = "Non précisée",
                        Url = r.url
                    }).ToList();

                    // Initialiser la plage de prix avec les données réelles
                    _prixAbsoluMin = _resultats.Min(r => r.Prix);
                    _prixAbsoluMax = _resultats.Max(r => r.Prix);
                    _prixMin = _prixAbsoluMin;
                    _prixMax = _prixAbsoluMax;

                    AfficherToast($"{_resultats.Count} résultat(s) trouvé(s)", "ws-toast-success");
                }
                else
                {
                    _resultats = new();
                    AfficherToast("Aucun résultat trouvé", "ws-toast-warning");
                }
            }
            catch (HttpRequestException)
            {
                AfficherToast("Impossible de contacter le service Python. Vérifiez que le serveur est lancé sur http://localhost:5000", "ws-toast-error");
            }
            catch (Exception ex)
            {
                AfficherToast($"Erreur : {ex.Message}", "ws-toast-error");
            }
            finally
            {
                _chargement = false;
                StateHasChanged();
            }
        }

        // ── Export CSV ──────────────────────────────────────────

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
                             .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
                             .ToArray();
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

        // ── Helpers ─────────────────────────────────────────────

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
    }
}