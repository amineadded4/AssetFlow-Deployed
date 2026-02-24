using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using AssetFlow.Application.DTOs;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class Fournisseurs : ComponentBase
    {
        [Inject]
        private AssetFlow.BlazorUI.Services.FournisseurService FournisseurSvc { get; set; } = default!;

        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        private class FournisseurVm
        {
            public int Id { get; set; }
            public string Nom { get; set; } = string.Empty;
            public string? Telephone { get; set; }
            public string? Adresse { get; set; }
            public string? Mail { get; set; }
            public int CommandesTotales { get; set; }
            public decimal TauxLivraisonATemps { get; set; }
            public decimal ScoreFiabilite { get; set; }
            public DateTime? DerniereCommande { get; set; }
            public bool Expanded { get; set; }
        }

        private class FormulaireVm
        {
            public int Id { get; set; }
            public string Nom { get; set; } = string.Empty;
            public string Telephone { get; set; } = string.Empty;
            public string Adresse { get; set; } = string.Empty;
            public string Mail { get; set; } = string.Empty;
            public int CommandesTotales { get; set; }
            public decimal TauxLivraisonATemps { get; set; }
            public decimal ScoreFiabilite { get; set; }
            public DateTime? DerniereCommande { get; set; }
        }

        private List<FournisseurVm> _tousLesFournisseurs = new();
        private List<FournisseurVm> _fournisseursAffiches = new();
        private bool _chargement = true;
        private string _erreurGlobale = string.Empty;
        private int _totalFournisseurs = 0;
        private decimal _meilleurScore = 0;
        private decimal _scoreMoyen = 0;
        private string _termeRecherche = string.Empty;
        private string _filtreScore = "all";
        private string _theme = "dark";
        private bool _sidebarOpen = false;
        private bool _panneauOuvert = false;
        private bool _modeModif = false;
        private bool _sauvegarde = false;
        private FormulaireVm _form = new();
        private Dictionary<string, string> _erreurs = new();
        private FournisseurVm? _fournisseurASupprimer;
        private string _toastMsg = string.Empty;
        private string _toastType = "toast-success";

        protected override async Task OnInitializedAsync()
        {
            // Lire l'état du thème depuis html.dark (géré par le bouton de index.html)
            var isDark = await JS.InvokeAsync<bool>("eval",
                "document.documentElement.classList.contains('dark')");
            _theme = isDark ? "dark" : "light";

            await ChargerFournisseurs();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            // Injecter une fonction globale, puis passer la ref séparément
            await JS.InvokeVoidAsync("eval", @"
                window.__assetflowSetThemeRef = function(ref) {
                    window.__blazorThemeRef = ref;
                    window.__themeObserver && window.__themeObserver.disconnect();
                    window.__themeObserver = new MutationObserver(function() {
                        var isDark = document.documentElement.classList.contains('dark');
                        window.__blazorThemeRef.invokeMethodAsync('OnThemeChanged', isDark);
                    });
                    window.__themeObserver.observe(document.documentElement, {
                        attributes: true, attributeFilter: ['class']
                    });
                };
            ");

            var dotNetRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("__assetflowSetThemeRef", dotNetRef);
        }

        private async Task ChargerFournisseurs()
        {
            _chargement = true;
            _erreurGlobale = string.Empty;
            try
            {
                var dtos = await FournisseurSvc.GetAllAsync();
                _tousLesFournisseurs = dtos.Select(d => new FournisseurVm
                {
                    Id = d.IdFournisseur,
                    Nom = d.Nom,
                    Telephone = d.Telephone,
                    Adresse = d.Adresse,
                    Mail = d.Mail,
                    CommandesTotales = d.CommandesTotales,
                    TauxLivraisonATemps = d.TauxLivraisonATemps,
                    ScoreFiabilite = d.ScoreFiabilite,
                    DerniereCommande = d.DerniereCommande
                }).ToList();
                RecalculerStats();
                AppliquerFiltres();
            }
            catch (Exception ex) { _erreurGlobale = $"Erreur : {ex.Message}"; }
            finally { _chargement = false; }
        }

        private void RecalculerStats()
        {
            _totalFournisseurs = _tousLesFournisseurs.Count;
            _meilleurScore = _tousLesFournisseurs.Any() ? _tousLesFournisseurs.Max(f => f.ScoreFiabilite) : 0;
            _scoreMoyen = _tousLesFournisseurs.Any() ? _tousLesFournisseurs.Average(f => f.ScoreFiabilite) : 0;
        }

        private void OnRecherche(ChangeEventArgs e)
        {
            _termeRecherche = e.Value?.ToString() ?? string.Empty;
            AppliquerFiltres();
        }

        private void AppliquerFiltreScore(string filtre)
        {
            _filtreScore = filtre;
            _tousLesFournisseurs.ForEach(f => f.Expanded = false);
            AppliquerFiltres();
        }

        private void AppliquerFiltres()
        {
            var q = _tousLesFournisseurs.AsEnumerable();
            q = _filtreScore switch
            {
                "excellent" => q.Where(f => f.ScoreFiabilite > 80),
                "moyen" => q.Where(f => f.ScoreFiabilite >= 50 && f.ScoreFiabilite <= 80),
                "critique" => q.Where(f => f.ScoreFiabilite < 50),
                _ => q
            };
            if (!string.IsNullOrWhiteSpace(_termeRecherche))
            {
                var t = _termeRecherche.Trim().ToLower();
                q = q.Where(f =>
                    f.Nom.ToLower().Contains(t) ||
                    (f.Telephone != null && f.Telephone.Contains(t)) ||
                    (f.Mail != null && f.Mail.ToLower().Contains(t)) ||
                    (f.Adresse != null && f.Adresse.ToLower().Contains(t)));
            }
            _fournisseursAffiches = q.ToList();
        }

        private void ToggleDetail(int id)
        {
            var cible = _tousLesFournisseurs.FirstOrDefault(f => f.Id == id);
            if (cible == null) return;
            bool was = cible.Expanded;
            _tousLesFournisseurs.ForEach(f => f.Expanded = false);
            if (!was) cible.Expanded = true;
        }

        private void OuvrirFormulaire(FournisseurVm? vm)
        {
            _erreurs.Clear();
            _modeModif = vm is not null;
            _panneauOuvert = true;
            _form = vm is not null
                ? new FormulaireVm
                {
                    Id = vm.Id,
                    Nom = vm.Nom,
                    Telephone = vm.Telephone ?? string.Empty,
                    Adresse = vm.Adresse ?? string.Empty,
                    Mail = vm.Mail ?? string.Empty,
                    CommandesTotales = vm.CommandesTotales,
                    TauxLivraisonATemps = vm.TauxLivraisonATemps,
                    ScoreFiabilite = vm.ScoreFiabilite,
                    DerniereCommande = vm.DerniereCommande
                }
                : new FormulaireVm();
        }

        private void FermerFormulaire() { _panneauOuvert = false; _erreurs.Clear(); }

        private async Task SauvegarderFournisseur()
        {
            _erreurs.Clear();
            if (string.IsNullOrWhiteSpace(_form.Nom)) _erreurs["Nom"] = "Le nom est obligatoire.";
            if (!string.IsNullOrWhiteSpace(_form.Mail) && !_form.Mail.Contains('@'))
                _erreurs["Mail"] = "E-mail invalide.";
            if (_form.ScoreFiabilite < 0 || _form.ScoreFiabilite > 100) _erreurs["Score"] = "Entre 0 et 100.";
            if (_form.TauxLivraisonATemps < 0 || _form.TauxLivraisonATemps > 100) _erreurs["Taux"] = "Entre 0 et 100.";
            if (_erreurs.Any()) return;

            _sauvegarde = true;
            try
            {
                if (_modeModif)
                {
                    var dto = new ModifierFournisseurDto
                    {
                        IdFournisseur = _form.Id,
                        Nom = _form.Nom.Trim(),
                        Telephone = Vide(_form.Telephone),
                        Adresse = Vide(_form.Adresse),
                        Mail = Vide(_form.Mail),
                        CommandesTotales = _form.CommandesTotales,
                        TauxLivraisonATemps = _form.TauxLivraisonATemps,
                        ScoreFiabilite = _form.ScoreFiabilite,
                        DerniereCommande = _form.DerniereCommande
                    };
                    var r = await FournisseurSvc.ModifierAsync(dto);
                    if (r.Succes)
                    {
                        // ── Mise à jour instantanée dans la liste locale ──
                        var vm = _tousLesFournisseurs.FirstOrDefault(f => f.Id == _form.Id);
                        if (vm != null)
                        {
                            vm.Nom = _form.Nom.Trim();
                            vm.Telephone = Vide(_form.Telephone);
                            vm.Adresse = Vide(_form.Adresse);
                            vm.Mail = Vide(_form.Mail);
                            vm.CommandesTotales = _form.CommandesTotales;
                            vm.TauxLivraisonATemps = _form.TauxLivraisonATemps;
                            vm.ScoreFiabilite = _form.ScoreFiabilite;
                            vm.DerniereCommande = _form.DerniereCommande;
                        }
                        RecalculerStats();
                        AppliquerFiltres();
                        FermerFormulaire();
                        AfficherToast($"« {_form.Nom} » mis à jour.", "toast-success");
                    }
                    else _erreurGlobale = r.Message;
                }
                else
                {
                    var dto = new CreerFournisseurDto
                    {
                        Nom = _form.Nom.Trim(),
                        Telephone = Vide(_form.Telephone),
                        Adresse = Vide(_form.Adresse),
                        Mail = Vide(_form.Mail),
                        CommandesTotales = _form.CommandesTotales,
                        TauxLivraisonATemps = _form.TauxLivraisonATemps,
                        ScoreFiabilite = _form.ScoreFiabilite,
                        DerniereCommande = _form.DerniereCommande
                    };
                    var r = await FournisseurSvc.AjouterAsync(dto);
                    if (r.Succes)
                    {
                        // ── Ajout instantané en tête de liste, sans rechargement API ──
                        _tousLesFournisseurs.Insert(0, new FournisseurVm
                        {
                            Id = r.IdFournisseur ?? 0,
                            Nom = _form.Nom.Trim(),
                            Telephone = Vide(_form.Telephone),
                            Adresse = Vide(_form.Adresse),
                            Mail = Vide(_form.Mail),
                            CommandesTotales = _form.CommandesTotales,
                            TauxLivraisonATemps = _form.TauxLivraisonATemps,
                            ScoreFiabilite = _form.ScoreFiabilite,
                            DerniereCommande = _form.DerniereCommande
                        });
                        RecalculerStats();
                        AppliquerFiltres();
                        FermerFormulaire();
                        AfficherToast($"« {_form.Nom} » ajouté.", "toast-success");
                    }
                    else _erreurGlobale = r.Message;
                }
            }
            catch (Exception ex) { _erreurGlobale = ex.Message; }
            finally { _sauvegarde = false; }
        }

        private void DemanderSuppression(FournisseurVm f) => _fournisseurASupprimer = f;
        private void AnnulerSuppression() => _fournisseurASupprimer = null;

        private async Task ConfirmerSuppression()
        {
            if (_fournisseurASupprimer == null) return;
            var nom = _fournisseurASupprimer.Nom;
            var id = _fournisseurASupprimer.Id;
            _fournisseurASupprimer = null;
            var r = await FournisseurSvc.SupprimerAsync(id);
            if (r.Succes)
            {
                _tousLesFournisseurs.RemoveAll(f => f.Id == id);
                RecalculerStats();
                AppliquerFiltres();
                AfficherToast($"« {nom} » supprimé.", "toast-success");
            }
            else _erreurGlobale = r.Message;
        }

        private void ToggleSidebar() => _sidebarOpen = !_sidebarOpen;

        // Le thème est contrôlé par le bouton de index.html (html.dark).
        // On lit son état au chargement dans OnInitializedAsync.
        // Si besoin de synchro en temps réel, appeler SyncTheme() depuis JS.
        private async Task SyncTheme()
        {
            var isDark = await JS.InvokeAsync<bool>("eval",
                "document.documentElement.classList.contains('dark')");
            _theme = isDark ? "dark" : "light";
            StateHasChanged();
        }

        /// <summary>Appelé depuis JS via MutationObserver quand html.dark change.</summary>
        [JSInvokable("OnThemeChanged")]
        public void OnThemeChanged(bool isDark)
        {
            _theme = isDark ? "dark" : "light";
            InvokeAsync(StateHasChanged);
        }

        private string RateClass(decimal r) => r >= 80 ? "good" : r >= 60 ? "avg" : "bad";
        private string ScoreFillClass(decimal s) => s >= 80 ? "green" : s >= 50 ? "amber" : "red";
        private string ScoreTextClass(decimal s) => s >= 80 ? "good" : s >= 50 ? "avg" : "bad";

        private async void AfficherToast(string msg, string type)
        {
            _toastMsg = msg;
            _toastType = type;
            StateHasChanged();
            await Task.Delay(3500);
            _toastMsg = string.Empty;
            StateHasChanged();
        }

        private static string? Vide(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    }
}