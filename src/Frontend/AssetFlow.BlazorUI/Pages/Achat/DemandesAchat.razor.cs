// ============================================================
// FICHIER  : Pages/Achat/DemandesAchat.razor.cs
// RÔLE     : Code-behind de la page Agent Achat — Demandes d'achat
// PATTERN  : partial class, même structure que Fournisseurs.razor.cs
// ============================================================

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class DemandesAchat : ComponentBase
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;

        // ─── Modèles internes ────────────────────────────────────

        private class OffreVm
        {
            public Guid    Id          { get; set; } = Guid.NewGuid();
            public string  NomFichier  { get; set; } = string.Empty;
            public long    Taille      { get; set; }
            public string? DataUrl     { get; set; }   // base64 pour l'aperçu iframe
            public bool    EstNouvelle { get; set; } = true;
        }

        private class ArticleVm
        {
            public string Designation { get; set; } = string.Empty;
            public string Categorie   { get; set; } = string.Empty;
            public int    Quantite    { get; set; }
        }

        private class DemandeVm
        {
            public int              Id                 { get; set; }
            public string           Reference          { get; set; } = string.Empty;
            public string           Titre              { get; set; } = string.Empty;
            public string           Description        { get; set; } = string.Empty;
            // Statuts : nouveau | en_cours | commande | traite | refuse
            public string           Statut             { get; set; } = "nouveau";
            public DateTime         DateCreation       { get; set; }
            public string           DemandeurNom       { get; set; } = string.Empty;
            public string           DemandeurInitiales { get; set; } = string.Empty;
            public string           DemandeurRole      { get; set; } = string.Empty;
            public string?          MotifRefus         { get; set; }
            public Guid?            OffreChoisieId     { get; set; }
            public List<ArticleVm>  Articles           { get; set; } = new();
            public List<OffreVm>    Offres             { get; set; } = new();
        }

        // ─── État de la page ─────────────────────────────────────

        private string _theme           = "dark";
        private bool   _sidebarOpen     = false;
        private string _nomUtilisateur  = "Agent Achat";
        private string _roleUtilisateur = "Service Achat";
        private string _initiales       = "AA";

        private List<DemandeVm> _demandes            = new();
        private DemandeVm?      _demandeSelectionnee = null;
        private string          _tabActive           = "tous";
        private string          _recherche           = string.Empty;
        private string          _motifRefus          = string.Empty;
        private OffreVm?        _offrePreview        = null;
        private string          _toastMsg            = string.Empty;
        private string          _toastType           = "toast-success";

        // ─── Propriété calculée — demandes filtrées ──────────────

        private IEnumerable<DemandeVm> DemandesFiltrees
        {
            get
            {
                var q = _demandes.AsEnumerable();

                q = _tabActive switch
                {
                    "nouveau"    => q.Where(d => d.Statut == "nouveau"),
                    "en_cours"   => q.Where(d => d.Statut == "en_cours"),
                    "commande"   => q.Where(d => d.Statut == "commande"),
                    "historique" => q.Where(d => d.Statut == "traite" || d.Statut == "refuse"),
                    _            => q.Where(d => d.Statut != "traite" && d.Statut != "refuse")
                };

                if (!string.IsNullOrWhiteSpace(_recherche))
                {
                    var t = _recherche.Trim().ToLower();
                    q = q.Where(d =>
                        d.Reference.ToLower().Contains(t)    ||
                        d.Titre.ToLower().Contains(t)        ||
                        d.DemandeurNom.ToLower().Contains(t));
                }

                return q.OrderByDescending(d => d.DateCreation);
            }
        }

        // ─── Lifecycle ───────────────────────────────────────────

        protected override async Task OnInitializedAsync()
        {
            // Lire le thème actuel depuis html.dark
            try
            {
                var isDark = await JS.InvokeAsync<bool>("eval",
                    "document.documentElement.classList.contains('dark')");
                _theme = isDark ? "dark" : "light";
            }
            catch { /* ignore si JS non disponible */ }

            // Charger l'utilisateur depuis le localStorage (même pattern que Fournisseurs)
            await ChargerInfosUtilisateur();

            // Données mock — remplacer par appel API quand le backend est prêt
            ChargerDonneesMock();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            // MutationObserver : écoute html.dark pour synchroniser le thème
            // même mécanisme que Fournisseurs, nom de variable différent pour éviter conflits
            try
            {
                await JS.InvokeVoidAsync("eval", @"
                    window.__assetflowThemeRefDA = null;
                    window.__assetflowSetThemeDA = function(ref) {
                        window.__assetflowThemeRefDA = ref;
                        if (window.__themeObsDA) window.__themeObsDA.disconnect();
                        window.__themeObsDA = new MutationObserver(function() {
                            var dark = document.documentElement.classList.contains('dark');
                            window.__assetflowThemeRefDA &&
                                window.__assetflowThemeRefDA.invokeMethodAsync('OnThemeChanged', dark);
                        });
                        window.__themeObsDA.observe(document.documentElement, {
                            attributes: true, attributeFilter: ['class']
                        });
                    };
                ");
                var dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("__assetflowSetThemeDA", dotNetRef);
            }
            catch { }
        }

        [JSInvokable("OnThemeChanged")]
        public void OnThemeChanged(bool isDark)
        {
            _theme = isDark ? "dark" : "light";
            InvokeAsync(StateHasChanged);
        }

        // ─── Chargement utilisateur ──────────────────────────────

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
                    _nomUtilisateur = SupprimerGuillemets(nom);
                    var parts = _nomUtilisateur.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                        _initiales = $"{parts[0][0]}{parts[1][0]}".ToUpper();
                    else if (parts.Length == 1 && parts[0].Length >= 2)
                        _initiales = parts[0][..2].ToUpper();
                }
                if (!string.IsNullOrWhiteSpace(role))
                    _roleUtilisateur = SupprimerGuillemets(role);
            }
            catch { }
        }

        private static string SupprimerGuillemets(string valeur)
        {
            valeur = valeur.Trim();
            if (valeur.Length >= 2 &&
                ((valeur.StartsWith('"') && valeur.EndsWith('"')) ||
                 (valeur.StartsWith('\'') && valeur.EndsWith('\''))))
                valeur = valeur[1..^1].Trim();
            return valeur;
        }

        // ─── Données mock ────────────────────────────────────────

        private void ChargerDonneesMock()
        {
            _demandes = new List<DemandeVm>
            {
                new DemandeVm
                {
                    Id = 1, Reference = "#PR-2024-042",
                    Titre = "Upgrade RAM Serveur Principal",
                    Description = "Le serveur SRV-DB-01 sature sa mémoire lors des pics de charge de l'après-midi. " +
                                  "Une extension de 128 Go (4×32 Go) est nécessaire pour maintenir les performances " +
                                  "de l'application ERP. Modèle compatible : DDR4 ECC 3200MHz.",
                    Statut        = "nouveau",
                    DateCreation  = DateTime.Now.AddHours(-2),
                    DemandeurNom  = "Marc Lefebvre", DemandeurInitiales = "ML",
                    DemandeurRole = "Infrastructure Engineer",
                    Articles = new List<ArticleVm>
                    {
                        new ArticleVm { Designation = "RAM 32GB DDR4-3200 ECC RDIMM",  Categorie = "Hardware",    Quantite = 4 },
                        new ArticleVm { Designation = "Kit d'installation Serveur Rack", Categorie = "Accessoires", Quantite = 1 }
                    }
                },
                new DemandeVm
                {
                    Id = 2, Reference = "#PR-2024-039",
                    Titre = "MacBook Pro 14 M3 Max",
                    Description = "Remplacement du MacBook Pro 2019 arrivé en fin de vie. " +
                                  "Configuration : 36 Go RAM, 1 To SSD, nécessaire pour développement iOS et rendu vidéo.",
                    Statut        = "en_cours",
                    DateCreation  = DateTime.Now.AddDays(-1),
                    DemandeurNom  = "Sophie Martin", DemandeurInitiales = "SM",
                    DemandeurRole = "Développeuse Mobile",
                    Articles = new List<ArticleVm>
                    {
                        new ArticleVm { Designation = "MacBook Pro 14\" M3 Max 36Go/1To", Categorie = "Informatique", Quantite = 1 }
                    },
                    Offres = new List<OffreVm>
                    {
                        new OffreVm { NomFichier = "offre_apple_store.pdf", Taille = 245_000, EstNouvelle = false },
                        new OffreVm { NomFichier = "offre_ldlc_pro.pdf",   Taille = 198_000, EstNouvelle = false }
                    }
                },
                new DemandeVm
                {
                    Id = 3, Reference = "#PR-2024-035",
                    Titre = "Écran Dell UltraSharp 32\"",
                    Description = "Écran 4K 32 pouces pour le poste du designer. " +
                                  "Dell UltraSharp U3223QE avec hub USB-C intégré.",
                    Statut        = "traite",
                    DateCreation  = DateTime.Parse("2024-10-24"),
                    DemandeurNom  = "Thomas Bernard", DemandeurInitiales = "TB",
                    DemandeurRole = "Designer UX",
                    Articles = new List<ArticleVm>
                    {
                        new ArticleVm { Designation = "Dell UltraSharp 32\" U3223QE", Categorie = "Périphériques", Quantite = 1 }
                    }
                },
                new DemandeVm
                {
                    Id = 4, Reference = "#PR-2024-031",
                    Titre = "Kit Clavier & Souris sans fil",
                    Description = "Renouvellement du matériel pour 5 postes de travail en open space. " +
                                  "Modèle : Logitech MK470.",
                    Statut        = "refuse",
                    DateCreation  = DateTime.Parse("2024-10-20"),
                    DemandeurNom  = "Léa Dubois", DemandeurInitiales = "LD",
                    DemandeurRole = "Assistante RH",
                    MotifRefus    = "Budget épuisé pour ce trimestre. Reporter au Q1 2025.",
                    Articles = new List<ArticleVm>
                    {
                        new ArticleVm { Designation = "Logitech MK470 Kit clavier+souris", Categorie = "Périphériques", Quantite = 5 }
                    }
                },
                new DemandeVm
                {
                    Id = 5, Reference = "#PR-2024-028",
                    Titre = "Switch réseau 48 ports",
                    Description = "Extension du réseau pour le nouveau plateau. " +
                                  "Switch manageable 48 ports Gigabit avec PoE+.",
                    Statut        = "commande",
                    DateCreation  = DateTime.Parse("2024-10-18"),
                    DemandeurNom  = "Marc Lefebvre", DemandeurInitiales = "ML",
                    DemandeurRole = "Infrastructure Engineer",
                    Articles = new List<ArticleVm>
                    {
                        new ArticleVm { Designation = "Cisco Catalyst 1300-48P-4X", Categorie = "Réseau", Quantite = 1 }
                    }
                }
            };
        }

        // ─── Actions utilisateur ─────────────────────────────────

        private void SelectionnerDemande(DemandeVm d)
        {
            _demandeSelectionnee = d;
            _motifRefus          = string.Empty;
        }

        private void SetTab(string tab)
        {
            _tabActive           = tab;
            _demandeSelectionnee = null;
            _motifRefus          = string.Empty;
        }

        private void OnRecherche(ChangeEventArgs e)
            => _recherche = e.Value?.ToString() ?? string.Empty;

        private async Task AjouterOffres(InputFileChangeEventArgs e, int demandeId)
        {
            var demande = _demandes.FirstOrDefault(d => d.Id == demandeId);
            if (demande == null) return;

            const long maxTaille = 10L * 1024 * 1024; // 10 Mo

            foreach (var fichier in e.GetMultipleFiles(10))
            {
                if (fichier.Size > maxTaille)
                {
                    AfficherToast($"« {fichier.Name} » dépasse 10 Mo.", "toast-error");
                    continue;
                }

                // Lire le fichier pour affichage dans la modale iframe
                string? dataUrl = null;
                try
                {
                    using var stream = fichier.OpenReadStream(maxTaille);
                    var bytes = new byte[fichier.Size];
                    _ = await stream.ReadAsync(bytes);
                    dataUrl = $"data:application/pdf;base64,{Convert.ToBase64String(bytes)}";
                }
                catch { /* pas d'aperçu si erreur lecture */ }

                demande.Offres.Add(new OffreVm
                {
                    NomFichier  = fichier.Name,
                    Taille      = fichier.Size,
                    DataUrl     = dataUrl,
                    EstNouvelle = true
                });
            }

            StateHasChanged();
        }

        private void SupprimerOffre(int demandeId, Guid offreId)
        {
            var demande = _demandes.FirstOrDefault(d => d.Id == demandeId);
            demande?.Offres.RemoveAll(o => o.Id == offreId);
        }

        private async Task EnvoyerOffres(int demandeId)
        {
            var demande = _demandes.FirstOrDefault(d => d.Id == demandeId);
            if (demande == null) return;

            // Marquer toutes les nouvelles offres comme envoyées
            demande.Offres.ForEach(o => o.EstNouvelle = false);

            // TODO: appel API — POST /api/demandes/{id}/offres pour persister les PDF
            AfficherToast("Offres envoyées à l'IT avec succès.", "toast-success");
            await Task.CompletedTask;
        }

        private void ChangerStatut(int demandeId, string nouveauStatut)
        {
            var demande = _demandes.FirstOrDefault(d => d.Id == demandeId);
            if (demande == null) return;

            demande.Statut = nouveauStatut;
            AfficherToast($"Statut mis à jour : {LibelleStatut(nouveauStatut)}", "toast-success");

            // Si traitée → basculer vers l'onglet Historique
            if (nouveauStatut == "traite")
            {
                _demandeSelectionnee = null;
                _tabActive           = "historique";
            }
        }

        private void RefuserDemande(int demandeId)
        {
            if (string.IsNullOrWhiteSpace(_motifRefus)) return;

            var demande = _demandes.FirstOrDefault(d => d.Id == demandeId);
            if (demande == null) return;

            demande.Statut     = "refuse";
            demande.MotifRefus = _motifRefus.Trim();

            // TODO: appel API — PUT /api/demandes/{id}/refus
            AfficherToast("Demande refusée et archivée.", "toast-error");
            _motifRefus          = string.Empty;
            _demandeSelectionnee = null;
            _tabActive           = "historique";
        }

        private void PrevisualiserOffre(OffreVm o) => _offrePreview = o;
        private void FermerPreview()               => _offrePreview = null;
        private void ToggleSidebar()               => _sidebarOpen  = !_sidebarOpen;

        // ─── Helpers ─────────────────────────────────────────────

        private static string LibelleStatut(string statut) => statut switch
        {
            "nouveau"  => "NOUVEAU",
            "en_cours" => "EN COURS",
            "commande" => "COMMANDÉE",
            "traite"   => "TRAITÉE",
            "refuse"   => "REFUSÉE",
            _          => statut.ToUpper()
        };

        private static string FormatDateCarte(DateTime d)
        {
            var diff = DateTime.Now - d;
            if (diff.TotalMinutes < 60) return $"Il y a {(int)diff.TotalMinutes} min";
            if (diff.TotalHours   < 24) return $"Il y a {(int)diff.TotalHours} h";
            if (diff.TotalDays    < 2)  return "Hier";
            return d.ToString("dd MMM");
        }

        private static string FormatTaille(long bytes)
        {
            if (bytes < 1024)       return $"{bytes} o";
            if (bytes < 1024*1024)  return $"{bytes / 1024} Ko";
            return $"{bytes / 1024 / 1024:0.0} Mo";
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
    }
}
