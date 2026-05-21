using AssetFlow.BlazorUI.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.SignalR.Client;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class Materiel : ComponentBase, IAsyncDisposable
    {
        [Inject] private AssetFlow.BlazorUI.Services.MaterielService    MaterielSvc    { get; set; } = default!;
        [Inject] private AssetFlow.BlazorUI.Services.CommandeService    CommandeSvc    { get; set; } = default!;
        [Inject] private AssetFlow.BlazorUI.Services.FournisseurService FournisseurSvc { get; set; } = default!;
        [Inject] private AssetFlow.BlazorUI.Services.ArticleService     ArticleSvc     { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private ILocalStorageService        LocalStorage     { get; set; } = default!;
        [Inject] private HttpClient Http { get; set; } = default!;

        // ── formulaire matériel ─────────────────────────────────
        private class FormulaireVm
        {
            public int      Id            { get; set; }
            public string   Reference     { get; set; } = string.Empty;
            public string   Designation   { get; set; } = string.Empty;
            public string?  Description   { get; set; }
            public string   Categorie     { get; set; } = string.Empty;
            public int      QuantiteMin   { get; set; }
            public string   Unite         { get; set; } = "pièce";
            public string?  ImageUrl      { get; set; }
        }

        // ── formulaire nouvelle commande ───────────────────────
        private class FormulaireCommandeVm
        {
            public string    NumeroCommande      { get; set; } = string.Empty;
            public int       FournisseurId       { get; set; }
            public string    NomFournisseurLibre { get; set; } = string.Empty;
            public int       QuantiteAchetee     { get; set; } = 1;
            public DateTime  DateAchat           { get; set; } = DateTime.Today;
            public DateTime? DateLivraison       { get; set; }
            public DateTime? DateFinGarantie     { get; set; }
            public List<string?> NumerosSerie    { get; set; } = new();
        }

        // ── formulaire modification commande ───────────────────
        private class FormulaireModifCommandeVm
        {
            public int       CommandeId          { get; set; }
            public string    NumeroCommande      { get; set; } = string.Empty;
            public int       FournisseurId       { get; set; }
            public string    NomFournisseurLibre { get; set; } = string.Empty;
            public DateTime  DateAchat           { get; set; } = DateTime.Today;
            public DateTime? DateLivraison       { get; set; }
            public DateTime? DateFinGarantie     { get; set; }
        }

        // ── Données ────────────────────────────────────────────────
        private List<LigneMaterielDto>  _toutesLignes    = new();
        private List<LigneMaterielDto>  _lignes          = new();
        private List<FournisseurDto>    _fournisseurs    = new();
        private List<string>            _categories      = new();
        private int                     _totalCount      = 0;
        private bool                    _chargement      = true;
        private string                  _erreur          = string.Empty;
        private string                  _termeRecherche  = string.Empty;
        private string                  _categorieFiltre = "all";
        private bool                    _sidebarOpen     = false;

        // Ligne dont les commandes sont affichées (toggle bouton "i" matériel)
        private int? _materielCommandesOuvert = null;

        // ── Panneau formulaire matériel ────────────────────────────
        private bool                       _panneauOuvert     = false;
        private bool                       _modeModif         = false;
        private bool                       _sauvegarde        = false;
        private FormulaireVm               _form              = new();
        private Dictionary<string, string> _erreurs           = new();
        private string                     _erreurFormulaire  = string.Empty;
        private bool                       _nouveauProduit    = false;
        private bool                       _avecCommandeModif = false;

        // Formulaire nouvelle commande (dans panneau matériel)
        private FormulaireCommandeVm       _formCommande      = new();
        private Dictionary<string, string> _erreursCommande   = new();

        // ── Panneau modification commande ──────────────────────────
        private bool                        _panneauModifCommandeOuvert = false;
        private bool                        _sauvegardeCommande         = false;
        private FormulaireModifCommandeVm   _formModifCommande          = new();
        private Dictionary<string, string>  _erreursModifCommande       = new();
        private string                      _erreurModifCommande        = string.Empty;

        // ── Image ──────────────────────────────────────────────────
        private string  _imagePreview = string.Empty;
        private string  _imageErreur  = string.Empty;
        private bool    _dragOver     = false;
        private string? _imageBase64  = null;
        private string? _imageMime    = null;

        // ── Suppression matériel ───────────────────────────────────
        private LigneMaterielDto? _materielASupprimer = null;

        // ── Suppression commande ───────────────────────────────────
        private (int CommandeId, string NumeroCommande, string NomMateriel)? _commandeASupprimer = null;
        // ── Configuration seuil ────────────────────────────────────
        private LigneMaterielDto? _materielSeuil   = null;
        private int               _seuilMin        = 0;
        private string            _seuilErrorMsg   = string.Empty;
        private bool              _isSavingSeuil   = false;

        // ── Panneau articles ───────────────────────────────────────
        private bool             _panneauArticlesOuvert = false;
        private string           _panneauArticlesTitre  = string.Empty;
        private List<ArticleDto> _articles              = new();
        private bool             _chargementArticles    = false;
        private string           _rechercheArticle      = string.Empty;

        // Édition numéro de série
        private int?   _editArticleId = null;
        private string _editArticleNs = string.Empty;

        // ── Toast ──────────────────────────────────────────────────
        private string _toastMsg  = string.Empty;
        private string _toastType = "toast-success";

        // ── SignalR ────────────────────────────────────────────────
        private HubConnection? _hubConnection;

        // ── User ───────────────────────────────────────────────────
        private string _currentUserName = "Utilisateur";
        private string _currentUserRole = "Équipe Achat";
        private string TabClass(bool condition) => condition ? "sk-ref-tab active" : "sk-ref-tab";
        // private string      _roleUtilisateur = "Service Achat";
        private bool _estAdmin => _currentUserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        private string CurrentUserInitials
        {
            get
            {
                var parts = _currentUserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
                if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
                return "AA";
            }
        }

        private IEnumerable<ArticleDto> ArticlesFiltres =>
            string.IsNullOrWhiteSpace(_rechercheArticle)
                ? _articles
                : _articles.Where(a =>
                    (a.NumeroSerie != null && a.NumeroSerie.Contains(_rechercheArticle, StringComparison.OrdinalIgnoreCase)) ||
                    a.NumeroCommande.Contains(_rechercheArticle, StringComparison.OrdinalIgnoreCase));

        protected override async Task OnInitializedAsync()
        {
            await ChargerInfosUtilisateur();
            await Task.WhenAll(ChargerDonnees(), ChargerFournisseurs());
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

            _hubConnection.On("DashboardUpdated", async () =>
            {
                await InvokeAsync(async () =>
                {
                    try
                    {
                        var liste = await CommandeSvc.GetLignesMaterielsAsync();
                        _toutesLignes = liste;
                        _totalCount   = liste.Count;
                        _categories   = liste
                            .Select(l => l.Categorie)
                            .Distinct().OrderBy(c => c).ToList();
                        AppliquerFiltres();
                    }
                    catch { /* silencieux */ }
                    finally
                    {
                        _chargement = false;
                        StateHasChanged();
                    }
                });
            });

            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinDashboard");
            }
            catch { /* reste statique si SignalR non dispo */ }
        }
        private async Task OuvrirSelecteurImage()
        {
            await JS.InvokeVoidAsync("eval", "document.getElementById('img-upload').click()");
        }
        private static string TruncateDescription(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";
        }

        // ── Chargement ─────────────────────────────────────────────
        private async Task ChargerDonnees()
        {
            _chargement = true; _erreur = string.Empty;
            try
            {
                var liste = await CommandeSvc.GetLignesMaterielsAsync();
                _toutesLignes = liste;
                _totalCount   = liste.Count;

                _categories = liste
                    .Select(l => l.Categorie)
                    .Distinct().OrderBy(c => c).ToList();

                AppliquerFiltres();
            }
            catch (Exception ex) { _erreur = $"Erreur de chargement : {ex.Message}"; }
            finally { _chargement = false; }
        }

        private async Task ChargerFournisseurs()
        {
            try { _fournisseurs = await FournisseurSvc.GetAllAsync(); }
            catch { }
        }

        private void AppliquerFiltres()
        {
            var q = _toutesLignes.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_termeRecherche))
            {
                var t = _termeRecherche.Trim().ToLower();
                q = q.Where(l =>
                    l.Designation.ToLower().Contains(t) ||
                    l.Reference.ToLower().Contains(t)   ||
                    (l.Description?.ToLower().Contains(t) ?? false) ||
                    l.Commandes.Any(c =>
                        c.NumeroCommande.ToLower().Contains(t) ||
                        c.NomFournisseur.ToLower().Contains(t)));
            }

            if (_categorieFiltre != "all")
                q = q.Where(l => l.Categorie.Equals(_categorieFiltre, StringComparison.OrdinalIgnoreCase));

            _lignes = q.ToList();
        }

        // ── Filtres ────────────────────────────────────────────────
        private void OnRecherche(ChangeEventArgs e)
        {
            _termeRecherche = e.Value?.ToString() ?? string.Empty;
            AppliquerFiltres();
        }

        private void OnCategorieChange(ChangeEventArgs e)
        {
            _categorieFiltre = e.Value?.ToString() ?? "all";
            AppliquerFiltres();
        }

        // ── Toggle commandes matériel (bouton "i" sur la ligne matériel) ──
        private void ToggleCommandesMateriel(int materielId)
        {
            _materielCommandesOuvert = _materielCommandesOuvert == materielId ? null : materielId;
        }

        // ── Formulaire matériel (Ajouter / Modifier matériel) ─────
        private void OuvrirFormulaire(LigneMaterielDto? lg)
        {
            _erreurs.Clear(); _erreursCommande.Clear();
            _erreurFormulaire = string.Empty; _imageErreur = string.Empty;
            _modeModif        = lg is not null;
            _panneauOuvert    = true;
            _nouveauProduit   = false;
            _avecCommandeModif = false;
            _formCommande     = new FormulaireCommandeVm();
            AjusterArticles(); 

            if (lg is not null)
            {
                _form = new FormulaireVm
                {
                    Id          = lg.MaterielId,
                    Reference   = lg.Reference,
                    Designation = lg.Designation,
                    Description = lg.Description,
                    Categorie   = lg.Categorie,
                    QuantiteMin = lg.QuantiteMin,
                    Unite       = lg.Unite,
                    ImageUrl    = lg.ImageUrl
                };
                _imagePreview = lg.ImageUrl ?? string.Empty;
            }
            else
            {
                _form = new FormulaireVm();
                _imagePreview = string.Empty;
            }
            _imageBase64 = null; _imageMime = null;
        }

        private void FermerFormulaire()
        {
            _panneauOuvert = false;
            _erreurs.Clear(); _erreursCommande.Clear();
            _erreurFormulaire = string.Empty;
            _imagePreview = string.Empty; _imageBase64 = null; _imageMime = null;
            _nouveauProduit = false; _avecCommandeModif = false;
        }

        // ── Sélection produit existant (mode création) ────────────
        private void OnProduitExistantChange(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var id) && id > 0)
            {
                var lg = _toutesLignes.FirstOrDefault(p => p.MaterielId == id);
                if (lg != null)
                {
                    _form = new FormulaireVm
                    {
                        Id          = lg.MaterielId,
                        Reference   = lg.Reference,
                        Designation = lg.Designation,
                        Description = lg.Description,
                        Categorie   = lg.Categorie,
                        QuantiteMin = lg.QuantiteMin,
                        Unite       = lg.Unite,
                        ImageUrl    = lg.ImageUrl
                    };
                    _imagePreview = lg.ImageUrl ?? string.Empty;
                }
            }
            else
            {
                _form = new FormulaireVm(); _imagePreview = string.Empty;
            }
        }

        private void OnFournisseurInput(ChangeEventArgs e)
        {
            var val = e.Value?.ToString() ?? string.Empty;
            _formCommande.NomFournisseurLibre = val;
            var match = _fournisseurs.FirstOrDefault(f => f.Nom.Equals(val, StringComparison.OrdinalIgnoreCase));
            _formCommande.FournisseurId = match?.IdFournisseur ?? 0;
        }

        private void OnFournisseurModifInput(ChangeEventArgs e)
        {
            var val = e.Value?.ToString() ?? string.Empty;
            _formModifCommande.NomFournisseurLibre = val;
            var match = _fournisseurs.FirstOrDefault(f => f.Nom.Equals(val, StringComparison.OrdinalIgnoreCase));
            _formModifCommande.FournisseurId = match?.IdFournisseur ?? 0;
        }

        private void AjusterArticles()
        {
            var qte = _formCommande.QuantiteAchetee;
            while (_formCommande.NumerosSerie.Count < qte) _formCommande.NumerosSerie.Add(null);
            while (_formCommande.NumerosSerie.Count > qte) _formCommande.NumerosSerie.RemoveAt(_formCommande.NumerosSerie.Count - 1);
        }

        private void OnQuantiteAcheteeChange(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var q) && q >= 0)
                _formCommande.QuantiteAchetee = q;
            AjusterArticles();
        }

        // ── Sauvegarde matériel ────────────────────────────────────
        private async Task Sauvegarder()
        {
            _erreurs.Clear(); _erreursCommande.Clear();
            _erreurFormulaire = string.Empty;

            if (_modeModif)
            {
                if (string.IsNullOrWhiteSpace(_form.Designation)) _erreurs["Designation"] = "Obligatoire.";
                if (string.IsNullOrWhiteSpace(_form.Reference))   _erreurs["Reference"]   = "Obligatoire.";
                if (string.IsNullOrWhiteSpace(_form.Categorie))   _erreurs["Categorie"]   = "Obligatoire.";

                if (_avecCommandeModif)
                {
                    if (string.IsNullOrWhiteSpace(_formCommande.NumeroCommande))
                        _erreursCommande["NumeroCommande"] = "Obligatoire.";
                    
                    if (_formCommande.QuantiteAchetee <= 0)
                        _erreursCommande["Quantite"] = "Doit être > 0.";
                    if (_formCommande.DateLivraison.HasValue && _formCommande.DateLivraison.Value <= _formCommande.DateAchat)
                        _erreursCommande["DateLivraison"] = "La date de livraison doit être supérieure à la date d'achat.";
                }
            }
            else
            {
                if (_nouveauProduit)
                {
                    if (string.IsNullOrWhiteSpace(_form.Designation)) _erreurs["Designation"] = "Obligatoire.";
                    if (string.IsNullOrWhiteSpace(_form.Reference))   _erreurs["Reference"]   = "Obligatoire.";
                    if (string.IsNullOrWhiteSpace(_form.Categorie))   _erreurs["Categorie"]   = "Obligatoire.";
                }
                else
                {
                    if (_form.Id == 0) _erreurs["Produit"] = "Sélectionnez un produit.";
                }
                if (string.IsNullOrWhiteSpace(_formCommande.NumeroCommande))
                    _erreursCommande["NumeroCommande"] = "Obligatoire.";
                
                if (_formCommande.QuantiteAchetee <= 0)
                    _erreursCommande["Quantite"] = "Doit être > 0.";
                if (_formCommande.DateLivraison.HasValue && _formCommande.DateLivraison.Value <= _formCommande.DateAchat)
                    _erreursCommande["DateLivraison"] = "La date de livraison doit être supérieure à la date d'achat.";
            }

            if (_erreurs.Any() || _erreursCommande.Any()) return;

            // Créer fournisseur si libre
            if (_formCommande.FournisseurId == 0 && !string.IsNullOrWhiteSpace(_formCommande.NomFournisseurLibre)
                && (!_modeModif || _avecCommandeModif))
            {
                var nf = await FournisseurSvc.AjouterAsync(new CreerFournisseurDto { Nom = _formCommande.NomFournisseurLibre.Trim() });
                if (nf.Succes && nf.IdFournisseur.HasValue)
                    _formCommande.FournisseurId = nf.IdFournisseur.Value;
                else { _erreurFormulaire = "Impossible de créer le fournisseur."; return; }
            }

            _sauvegarde = true;
            try
            {
                int materielId;

                if (_modeModif)
                {
                    var currentQty = _toutesLignes.FirstOrDefault(l => l.MaterielId == _form.Id)?.QuantiteStock ?? 0;
                    var result = await MaterielSvc.ModifierAsync(new ModifierMaterielDto
                    {
                        Utilisateur   = _currentUserName,
                        Id            = _form.Id,
                        Reference     = _form.Reference.Trim(),
                        Designation   = _form.Designation.Trim(),
                        Description   = Vide(_form.Description),
                        Categorie     = _form.Categorie.Trim(),
                        QuantiteStock = currentQty,
                        QuantiteMin   = _form.QuantiteMin,
                        Unite         = (_form.Unite ?? "pièce").Trim(),
                        ImageUrl      = string.IsNullOrEmpty(_imagePreview) ? Vide(_form.ImageUrl) : _imagePreview
                    });
                    if (!result.Succes) { _erreurFormulaire = result.Message; return; }
                    materielId = _form.Id;

                    if (_avecCommandeModif)
                    {
                        AjusterArticles();
                        var cmd = await CommandeSvc.CreerAsync(new CreerCommandeDto
                        {
                            Utilisateur = _currentUserName,
                            NumeroCommande  = _formCommande.NumeroCommande.Trim(),
                            MaterielId      = materielId,
                            FournisseurId   = _formCommande.FournisseurId,
                            QuantiteAchetee = _formCommande.QuantiteAchetee,
                            DateAchat       = _formCommande.DateAchat,
                            DateLivraison   = _formCommande.DateLivraison,
                            DateFinGarantie = _formCommande.DateFinGarantie,
                            NumerosSerie    = _formCommande.NumerosSerie
                        });
                        if (!cmd.Succes) { _erreurFormulaire = cmd.Message; return; }
                    }
                    AfficherToast($"« {_form.Designation} » mis à jour.", "toast-success");
                }
                else
                {
                    if (_nouveauProduit)
                    {
                        var result = await MaterielSvc.AjouterAsync(new CreerMaterielDto
                        {
                            Utilisateur = _currentUserName,
                            Reference     = _form.Reference.Trim(),
                            Designation   = _form.Designation.Trim(),
                            Description   = Vide(_form.Description),
                            Categorie     = _form.Categorie.Trim(),
                            QuantiteStock = 0,
                            QuantiteMin   = _form.QuantiteMin,
                            Unite         = (_form.Unite ?? "pièce").Trim(),
                            ImageUrl      = string.IsNullOrEmpty(_imagePreview) ? null : _imagePreview
                        });
                        if (!result.Succes) { _erreurFormulaire = result.Message; return; }
                        materielId = result.IdMateriel!.Value;
                    }
                    else { materielId = _form.Id; }

                    AjusterArticles();
                    var cmd = await CommandeSvc.CreerAsync(new CreerCommandeDto
                    {
                        Utilisateur = _currentUserName,
                        NumeroCommande  = _formCommande.NumeroCommande.Trim(),
                        MaterielId      = materielId,
                        FournisseurId   = _formCommande.FournisseurId,
                        QuantiteAchetee = _formCommande.QuantiteAchetee,
                        DateAchat       = _formCommande.DateAchat,
                        DateLivraison   = _formCommande.DateLivraison,
                        DateFinGarantie = _formCommande.DateFinGarantie,
                        NumerosSerie    = _formCommande.NumerosSerie
                    });
                    if (!cmd.Succes) { _erreurFormulaire = cmd.Message; return; }
                    AfficherToast($"Commande {_formCommande.NumeroCommande} ajoutée.", "toast-success");
                }

                FermerFormulaire();
                await ChargerDonnees();
            }
            catch (Exception ex) { _erreurFormulaire = ex.Message; }
            finally { _sauvegarde = false; }
        }

        // ── Panneau modification commande ──────────────────────────
        private void OuvrirModifCommande(CommandeDto cmd)
        {
            _erreursModifCommande.Clear();
            _erreurModifCommande = string.Empty;
            _formModifCommande = new FormulaireModifCommandeVm
            {
                CommandeId          = cmd.Id,
                NumeroCommande      = cmd.NumeroCommande,
                FournisseurId       = cmd.FournisseurId,
                NomFournisseurLibre = cmd.NomFournisseur,
                DateAchat           = cmd.DateAchat,
                DateLivraison       = cmd.DateLivraison,
                DateFinGarantie     = cmd.DateFinGarantie
            };
            _panneauModifCommandeOuvert = true;
        }

        private void FermerModifCommande()
        {
            _panneauModifCommandeOuvert = false;
            _erreursModifCommande.Clear();
            _erreurModifCommande = string.Empty;
        }

        private async Task SauvegarderModifCommande()
        {
            _erreursModifCommande.Clear();
            _erreurModifCommande = string.Empty;

            if (string.IsNullOrWhiteSpace(_formModifCommande.NumeroCommande))
                _erreursModifCommande["NumeroCommande"] = "Obligatoire.";
          
            if (_formModifCommande.DateLivraison.HasValue && _formModifCommande.DateLivraison.Value <= _formModifCommande.DateAchat)
                _erreursModifCommande["DateLivraison"] = "La date de livraison doit être supérieure à la date d'achat.";

            if (_erreursModifCommande.Any()) return;

            _sauvegardeCommande = true;
            try
            {
                var dto = new ModifierCommandeDto
                {
                    Utilisateur = _currentUserName,
                    Id                  = _formModifCommande.CommandeId,
                    NumeroCommande      = _formModifCommande.NumeroCommande.Trim(),
                    FournisseurId       = _formModifCommande.FournisseurId,
                    NomFournisseurLibre = _formModifCommande.NomFournisseurLibre.Trim(),
                    DateAchat           = _formModifCommande.DateAchat,
                    DateLivraison       = _formModifCommande.DateLivraison,
                    DateFinGarantie     = _formModifCommande.DateFinGarantie
                };

                var result = await CommandeSvc.ModifierAsync(_formModifCommande.CommandeId, dto);
                if (result.Succes)
                {
                    AfficherToast($"Commande {dto.NumeroCommande} modifiée.", "toast-success");
                    FermerModifCommande();
                    await ChargerDonnees();
                }
                else { _erreurModifCommande = result.Message; }
            }
            catch (Exception ex) { _erreurModifCommande = ex.Message; }
            finally { _sauvegardeCommande = false; }
        }

        // ── Suppression matériel (cascade complète) ────────────────
        private void DemanderSuppressionMateriel(LigneMaterielDto lg)
            => _materielASupprimer = lg;

        private void AnnulerSuppressionMateriel()
            => _materielASupprimer = null;

        private async Task ConfirmerSuppressionMateriel()
        {
            if (_materielASupprimer is null) return;
            var lg = _materielASupprimer;
            _materielASupprimer = null;
            try
            {
                var result = await MaterielSvc.SupprimerAvecCascadeAsync(lg.MaterielId);
                if (result.Succes)
                {
                    AfficherToast($"« {lg.Designation} » supprimé avec toutes ses données.", "toast-success");
                    _materielCommandesOuvert = null;
                    await ChargerDonnees();
                }
                else AfficherToast(result.Message, "toast-error");
            }
            catch (Exception ex) { AfficherToast($"Erreur : {ex.Message}", "toast-error"); }
        }

        // ── Suppression commande ───────────────────────────────────
        private void DemanderSuppressionCommande(CommandeDto cmd, string nomMateriel)
            => _commandeASupprimer = (cmd.Id, cmd.NumeroCommande, nomMateriel);

        private void AnnulerSuppressionCommande()
            => _commandeASupprimer = null;

        private async Task ConfirmerSuppressionCommande()
        {
            if (_commandeASupprimer is null) return;
            var (commandeId, numCmd, _) = _commandeASupprimer.Value;
            _commandeASupprimer = null;
            try
            {
                var result = await CommandeSvc.SupprimerAsync(commandeId);
                if (result.Succes)
                {
                    AfficherToast($"Commande {numCmd} supprimée.", "toast-success");
                    await ChargerDonnees();
                }
                else AfficherToast(result.Message, "toast-error");
            }
            catch (Exception ex) { AfficherToast($"Erreur : {ex.Message}", "toast-error"); }
        }

        // ── Panneau articles matériel (bouton ···) ─────────────────
        private async Task OuvrirArticlesMateriel(LigneMaterielDto lg)
        {
            _panneauArticlesTitre  = $"{lg.Reference} — {lg.Designation}";
            _panneauArticlesOuvert = true;
            _chargementArticles    = true;
            _articles              = new();
            _rechercheArticle      = string.Empty;
            _editArticleId         = null;
            StateHasChanged();
            try { _articles = await CommandeSvc.GetArticlesByMaterielAsync(lg.MaterielId); }
            catch { }
            finally { _chargementArticles = false; }
        }

        // ── Panneau articles par commande (bouton "i" commande) ────
        private async Task OuvrirArticlesCommande(CommandeDto cmd)
        {
            _panneauArticlesTitre  = $"{cmd.NumeroCommande} — articles";
            _panneauArticlesOuvert = true;
            _chargementArticles    = true;
            _articles              = new();
            _rechercheArticle      = string.Empty;
            _editArticleId         = null;
            StateHasChanged();
            try { _articles = await CommandeSvc.GetArticlesByCommandeAsync(cmd.Id); }
            catch { }
            finally { _chargementArticles = false; }
        }

        private void FermerArticles()
        {
            _panneauArticlesOuvert = false;
            _editArticleId = null;
        }

        // ── Édition numéro de série ────────────────────────────────
        private void OuvrirEditArticle(ArticleDto art)
        {
            _editArticleId = art.Id;
            _editArticleNs = art.NumeroSerie ?? string.Empty;
        }
        private void AnnulerEditArticle() { _editArticleId = null; _editArticleNs = string.Empty; }

        private async Task SauvegarderNumeroSerie(int articleId)
        {
            try
            {
                var result = await ArticleSvc.UpdateNumeroSerieAsync(articleId, _editArticleNs);
                if (result)
                {
                    var art = _articles.FirstOrDefault(a => a.Id == articleId);
                    if (art != null) art.NumeroSerie = string.IsNullOrWhiteSpace(_editArticleNs) ? null : _editArticleNs.Trim();
                    AfficherToast("Numéro de série mis à jour.", "toast-success");
                }
                else AfficherToast("Erreur lors de la mise à jour.", "toast-error");
            }
            catch (Exception ex) { AfficherToast($"Erreur : {ex.Message}", "toast-error"); }
            finally { _editArticleId = null; _editArticleNs = string.Empty; }
        }

        // ── Export ─────────────────────────────────────────────────
        private async Task ExporterExcel()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Référence;Désignation;Catégorie;Stock actuel;N° Commande;Fournisseur;Qté achetée;Date achat;Date livraison;Date fin garantie");
                foreach (var l in _lignes)
                {
                    if (l.Commandes.Count == 0)
                        sb.AppendLine($"{Csv(l.Reference)};{Csv(l.Designation)};{Csv(l.Categorie)};{l.QuantiteStock};;;;");
                    else
                        foreach (var c in l.Commandes)
                            sb.AppendLine($"{Csv(l.Reference)};{Csv(l.Designation)};{Csv(l.Categorie)};{l.QuantiteStock};{Csv(c.NumeroCommande)};{Csv(c.NomFournisseur)};{c.QuantiteAchetee};{c.DateAchat:dd/MM/yyyy};{c.DateLivraison?.ToString("dd/MM/yyyy") ?? ""};{c.DateFinGarantie?.ToString("dd/MM/yyyy") ?? ""}");
                }
                var bytes   = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                var b64     = Convert.ToBase64String(bytes);
                var fn      = $"materiels_{DateTime.Now:yyyyMMdd_HHmm}.csv";
                await JS.InvokeVoidAsync("eval", $@"(function(){{var a=document.createElement('a');a.href='data:text/csv;base64,{b64}';a.download='{fn}';document.body.appendChild(a);a.click();document.body.removeChild(a);}})();");
                AfficherToast("Export téléchargé.", "toast-success");
            }
            catch (Exception ex) { AfficherToast($"Erreur : {ex.Message}", "toast-error"); }
        }

        private async Task ExporterPdf()
        {
            try
            {
                var rows = new System.Text.StringBuilder();
                foreach (var l in _lignes)
                {
                    if (l.Commandes.Count == 0)
                        rows.AppendLine($"<tr><td>{HE(l.Reference)}</td><td>{HE(l.Designation)}</td><td>{HE(l.Categorie)}</td><td>{l.QuantiteStock}</td><td>—</td><td>—</td><td>—</td><td>—</td></tr>");
                    else
                        foreach (var c in l.Commandes)
                            rows.AppendLine($"<tr><td>{HE(l.Reference)}</td><td>{HE(l.Designation)}</td><td>{HE(l.Categorie)}</td><td>{l.QuantiteStock}</td><td>{HE(c.NumeroCommande)}</td><td>{HE(c.NomFournisseur)}</td><td>{c.QuantiteAchetee}</td><td>{c.DateAchat:dd/MM/yyyy}</td></tr>");
                }
                var html = $@"<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Matériels</title><style>body{{font-family:Arial;font-size:11px;margin:20px}}table{{width:100%;border-collapse:collapse}}th{{background:#136dec;color:#fff;padding:7px 8px;font-size:10px;text-transform:uppercase}}td{{padding:6px 8px;border-bottom:1px solid #eee;font-size:10px}}tr:nth-child(even){{background:#f8fafc}}</style></head><body><h2>Catalogue Matériels</h2><p>Exporté le {DateTime.Now:dd/MM/yyyy HH:mm}</p><table><thead><tr><th>Référence</th><th>Désignation</th><th>Catégorie</th><th>Stock</th><th>N° Commande</th><th>Fournisseur</th><th>Qté</th><th>Date achat</th></tr></thead><tbody>{rows}</tbody></table></body></html>";
                await JS.InvokeVoidAsync("eval", $@"(function(){{var w=window.open('','_blank','width=900,height=700');w.document.write({System.Text.Json.JsonSerializer.Serialize(html)});w.document.close();w.focus();setTimeout(function(){{w.print();}},400);}})();");
                AfficherToast("PDF ouvert.", "toast-success");
            }
            catch (Exception ex) { AfficherToast($"Erreur : {ex.Message}", "toast-error"); }
        }

        // ── Sidebar ─────────────────────────────────────────────────
        private void ToggleSidebar() => _sidebarOpen = !_sidebarOpen;

        // ── Upload image ────────────────────────────────────────────
        private async Task OnImageSelected(InputFileChangeEventArgs e)
        {
            _imageErreur = string.Empty;
            var file = e.File;
            if (file is null) return;
            if (file.Size > 2 * 1024 * 1024) { _imageErreur = "Max 2 Mo."; return; }
            if (!new[] { "image/jpeg", "image/png", "image/webp" }.Contains(file.ContentType))
            { _imageErreur = "Format non supporté."; return; }
            try
            {
                using var stream = file.OpenReadStream(2 * 1024 * 1024);
                using var ms     = new MemoryStream();
                await stream.CopyToAsync(ms);
                var bytes     = ms.ToArray();
                _imageBase64  = Convert.ToBase64String(bytes);
                _imageMime    = file.ContentType;
                _imagePreview = $"data:{_imageMime};base64,{_imageBase64}";
                _form.ImageUrl = _imagePreview;
            }
            catch (Exception ex) { _imageErreur = ex.Message; }
        }

        private void OnDragOver()  => _dragOver = true;
        private void OnDragLeave() => _dragOver = false;
        private void SupprimerImage() { _imagePreview = string.Empty; _imageBase64 = null; _imageMime = null; _form.ImageUrl = null; }

        // ── Helpers ─────────────────────────────────────────────────
        private static string StatutArticleClass(string s) => s switch
        {
            "Disponible"   => "art-disponible",
            "Affecte"      => "art-affecte",
            "HorsService"  => "art-hors",
            "EnReparation" => "art-rep",
            _              => "art-disponible"
        };
        private static string FormatStatut(string s) => s switch
        {
            "HorsService"  => "Hors Service",
            "EnReparation" => "En Réparation",
            _              => s
        };
        private static string CatBadgeClass(string cat) => cat.ToLower() switch
        {
            var c when c.Contains("périph") || c.Contains("periph") => "cat-teal",
            var c when c.Contains("infor")                          => "cat-purple",
            var c when c.Contains("mobil")                          => "cat-amber",
            var c when c.Contains("électro") || c.Contains("electro") => "cat-blue",
            _                                                        => "cat-default"
        };
        private static string? Vide(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
        private static string Csv(string v) => v.Contains(';') || v.Contains('"') ? $"\"{v.Replace("\"", "\"\"")}\"" : v;
        private static string HE(string v) => v.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;");

        private async void AfficherToast(string msg, string type)
        {
            _toastMsg = msg; _toastType = type; StateHasChanged();
            await Task.Delay(3500);
            _toastMsg = string.Empty; StateHasChanged();
        }

        private async Task ChargerInfosUtilisateur()
        {
            try
            {
                var nom  = await JS.InvokeAsync<string?>("eval", "localStorage.getItem('user_name')");
                var role = await JS.InvokeAsync<string?>("eval", "localStorage.getItem('user_role')");
                if (!string.IsNullOrWhiteSpace(nom))  _currentUserName = Nettoy(nom);
                if (!string.IsNullOrWhiteSpace(role)) _currentUserRole = Nettoy(role);
            }
            catch { }
        }
        private static string Nettoy(string v)
        {
            v = v.Trim();
            if (v.Length >= 2 && ((v.StartsWith('"') && v.EndsWith('"')) || (v.StartsWith('\'') && v.EndsWith('\''))))
                v = v[1..^1].Trim();
            return v;
        }

        // ── Modal configuration seuil ─────────────────────────────
        private void OuvrirSeuil(LigneMaterielDto lg)
        {
            _materielSeuil = lg;
            _seuilMin      = lg.QuantiteMin;
            _seuilErrorMsg = string.Empty;
        }

        private void FermerSeuil()
        {
            _materielSeuil = null;
            _seuilErrorMsg  = string.Empty;
        }

        private async Task SauvegarderSeuil()
        {
            if (_materielSeuil == null) return;
            if (_seuilMin < 0) { _seuilErrorMsg = "Le seuil doit être positif."; return; }

            _isSavingSeuil = true;
            try
            {
                var lg = _toutesLignes.FirstOrDefault(l => l.MaterielId == _materielSeuil.MaterielId);
                var result = await MaterielSvc.ModifierAsync(new ModifierMaterielDto
                {
                    Utilisateur   = _currentUserName,
                    Id            = _materielSeuil.MaterielId,
                    Reference     = _materielSeuil.Reference,
                    Designation   = _materielSeuil.Designation,
                    Description   = _materielSeuil.Description,
                    Categorie     = _materielSeuil.Categorie,
                    QuantiteStock = _materielSeuil.QuantiteStock,
                    QuantiteMin   = _seuilMin,
                    Unite         = _materielSeuil.Unite,
                    ImageUrl      = _materielSeuil.ImageUrl
                });

                if (result.Succes)
                {
                    AfficherToast($"Seuil de « {_materielSeuil.Designation} » mis à jour.", "toast-success");
                    FermerSeuil();
                    await ChargerDonnees();
                }
                else { _seuilErrorMsg = result.Message; }
            }
            catch (Exception ex) { _seuilErrorMsg = ex.Message; }
            finally { _isSavingSeuil = false; StateHasChanged(); }
        }

        private int GetZonePct(int from, int to)
        {
            var total = Math.Max(1, to);
            return Math.Min(100, Math.Max(0, (int)((double)(to - from) / total * 100)));
        }
        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                try { await _hubConnection.InvokeAsync("LeaveDashboard"); } catch { }
                await _hubConnection.DisposeAsync();
            }
        }
    }

}