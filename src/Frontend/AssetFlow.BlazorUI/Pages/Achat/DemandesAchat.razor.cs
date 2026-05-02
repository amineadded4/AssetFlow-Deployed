using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using AssetFlow.BlazorUI.DTOs;
using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class DemandesAchat : ComponentBase
    {
        [Inject] private IJSRuntime          JS              { get; set; } = default!;
        [Inject] private DemandeAchatService DemandeAchatSvc { get; set; } = default!;
        [Inject] private HttpClient          _http           { get; set; } = default!;
        [Inject] private NavigationManager Navigation    { get; set; } = default!;
        private HubConnection? _hubConnection;

        // ── ViewModels ───────────────────────────────────────────

        private class LigneVm
        {
            public int     IdLigne     { get; set; }
            public string  Reference   { get; set; } = string.Empty;
            public string  NomProduit  { get; set; } = string.Empty;
            public int     Quantite    { get; set; }
            public string? Description { get; set; }
        }

        private class OffreVm
        {
            public Guid    Id         { get; set; }
            public int     IdDemande  { get; set; }
            public string  NomFichier { get; set; } = string.Empty;
            public long    Taille     { get; set; }
            public bool    EstChoisie { get; set; }
            public string? PdfUrl     { get; set; }
        }

        private class DemandeVm
        {
            public int           Id           { get; set; }
            public string        Reference    { get; set; } = string.Empty;
            public string        NomProduit   { get; set; } = string.Empty;
            public int           Quantite     { get; set; }
            public string?       Description  { get; set; }
            public string        Statut       { get; set; } = "en_attente";
            public DateTime      DateCreation { get; set; }
            public string        DemandeurNom { get; set; } = string.Empty;
            public string        Initiales    { get; set; } = string.Empty;
            public string?       MotifRefus   { get; set; }
            public List<LigneVm> Lignes       { get; set; } = new();
            public List<OffreVm> Offres       { get; set; } = new();
            public DateTime? VuParAchatLe { get; set; }
        }

        // ── État ─────────────────────────────────────────────────

        private string      _theme           = "dark";
        private bool        _sidebarOpen     = false;
        private string      _nomUtilisateur  = "Agent Achat";
        private string      _roleUtilisateur = "Service Achat";
        private bool _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        private string      _initiales       = "AA";
        private bool        _chargement      = true;

        private List<DemandeVm> _demandes            = new();
        private DemandeVm?      _demandeSelectionnee = null;
        private string          _tabActive           = "tous";
        private string          _recherche           = string.Empty;
        private string          _motifRefus          = string.Empty;
        private OffreVm?        _offrePreview        = null;
        private string          _toastMsg            = string.Empty;
        private string          _toastType           = "toast-success";
        private int _countNonVus = 0;

        // ── Computed ─────────────────────────────────────────────

        private IEnumerable<DemandeVm> DemandesFiltrees
        {
            get
            {
                var q = _demandes.AsEnumerable();

                q = _tabActive switch
                {
                    "en_attente"          => q.Where(d => d.Statut == "en_attente"),
                    "en_cours_traitement" => q.Where(d => d.Statut == "en_cours_traitement"),
                    "commande"            => q.Where(d => d.Statut == "commande"),
                    "historique"          => q.Where(d => d.Statut == "traite" || d.Statut == "refuse"),
                    _                     => q.Where(d => d.Statut != "traite" && d.Statut != "refuse")
                };

                if (!string.IsNullOrWhiteSpace(_recherche))
                {
                    var t = _recherche.Trim().ToLower();
                    q = q.Where(d =>
                        d.Reference.ToLower().Contains(t)    ||
                        d.NomProduit.ToLower().Contains(t)   ||
                        d.DemandeurNom.ToLower().Contains(t) ||
                        d.Lignes.Any(l => l.NomProduit.ToLower().Contains(t) ||
                                         l.Reference.ToLower().Contains(t)));
                }

                return q.OrderByDescending(d => d.DateCreation);
            }
        }

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
            await ChargerDemandesAsync();
            _countNonVus = await DemandeAchatSvc.GetCountNonVusAsync();
            await ConnecterSignalR();
        }

        private async Task ConnecterSignalR()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5235/dashboardhub", options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        try { return await JS.InvokeAsync<string?>("eval",
                            "localStorage.getItem('access_token') || localStorage.getItem('token')"); }
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
                        var dtos = await DemandeAchatSvc.GetAllAsync();
                        _demandes = dtos.Select(MapDtoVersVm).ToList();
                        // Resynchroniser la demande sélectionnée
                        if (_demandeSelectionnee != null)
                            _demandeSelectionnee = _demandes.FirstOrDefault(d => d.Id == _demandeSelectionnee.Id);
                        _countNonVus = await DemandeAchatSvc.GetCountNonVusAsync();
                    }
                    catch { }
                    finally { StateHasChanged(); }
                });
            });

            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinDashboard");
            }
            catch { }
        }

        // Remplacer le DisposeAsync existant :
        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                try { await _hubConnection.InvokeAsync("LeaveDashboard"); } catch { }
                await _hubConnection.DisposeAsync();
            }
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;
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

        // ── Chargement ───────────────────────────────────────────

        private async Task ChargerDemandesAsync()
        {
            _chargement = true;
            StateHasChanged();
            var dtos = await DemandeAchatSvc.GetAllAsync();
            _demandes = dtos.Select(MapDtoVersVm).ToList();
            _chargement = false;
            StateHasChanged();
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

        // ── Actions ──────────────────────────────────────────────

        private async Task SelectionnerDemande(DemandeVm d)
        {
            _demandeSelectionnee = d;
            _motifRefus          = string.Empty;
            if (d.VuParAchatLe == null)
            {
                d.VuParAchatLe = DateTime.UtcNow; // optimistic update
                await DemandeAchatSvc.MarquerVuAsync(d.Id);
                // mettre à jour le badge sidebar
                _countNonVus = Math.Max(0, _countNonVus - 1);
                StateHasChanged();
            }
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

            const long maxTaille = 10L * 1024 * 1024;

            foreach (var fichier in e.GetMultipleFiles(10))
            {
                if (fichier.Size > maxTaille)
                {
                    AfficherToast($"« {fichier.Name} » dépasse 10 Mo.", "toast-error");
                    continue;
                }

                byte[] bytes;
                try
                {
                    using var stream = fichier.OpenReadStream(maxTaille);
                    bytes = new byte[fichier.Size];
                    _ = await stream.ReadAsync(bytes);
                }
                catch
                {
                    AfficherToast($"Erreur lecture « {fichier.Name} ».", "toast-error");
                    continue;
                }

                var offreVm = new OffreVm
                {
                    Id = Guid.NewGuid(), IdDemande = demandeId,
                    NomFichier = fichier.Name, Taille = fichier.Size, EstChoisie = false
                };
                demande.Offres.Add(offreVm);
                StateHasChanged();

                var reponse = await DemandeAchatSvc.AjouterOffreAsync(demandeId, fichier.Name, bytes);

                if (reponse.Succes)
                {
                    var demandeActualisee = await DemandeAchatSvc.GetByIdAsync(demandeId);
                    if (demandeActualisee != null)
                    {
                        var idx = _demandes.FindIndex(d => d.Id == demandeId);
                        if (idx >= 0)
                        {
                            var vm = MapDtoVersVm(demandeActualisee);
                            _demandes[idx] = vm;
                            if (_demandeSelectionnee?.Id == demandeId)
                                _demandeSelectionnee = vm;
                        }
                    }
                    AfficherToast($"Offre « {fichier.Name} » ajoutée.", "toast-success");
                }
                else
                {
                    demande.Offres.Remove(offreVm);
                    AfficherToast($"Erreur : {reponse.Message}", "toast-error");
                }
            }
            StateHasChanged();
        }

        private async Task SupprimerOffre(int demandeId, Guid offreId)
        {
            var demande = _demandes.FirstOrDefault(d => d.Id == demandeId);
            if (demande == null) return;

            var offre = demande.Offres.FirstOrDefault(o => o.Id == offreId);
            demande.Offres.RemoveAll(o => o.Id == offreId);
            StateHasChanged();

            var reponse = await DemandeAchatSvc.SupprimerOffreAsync(demandeId, offreId);
            if (!reponse.Succes)
            {
                if (offre != null) demande.Offres.Add(offre);
                AfficherToast($"Erreur : {reponse.Message}", "toast-error");
            }
        }

        private async Task ChangerStatut(int demandeId, string nouveauStatut)
        {
            var demande = _demandes.FirstOrDefault(d => d.Id == demandeId);
            if (demande == null) return;

            var ancien     = demande.Statut;
            demande.Statut = nouveauStatut;
            StateHasChanged();
            var reponse = await DemandeAchatSvc.ChangerStatutAsync(demandeId, nouveauStatut, _nomUtilisateur);

            if (reponse.Succes)
            {
                AfficherToast($"Statut mis à jour : {LibelleStatut(nouveauStatut)}", "toast-success");
                if (nouveauStatut == "traite")
                {
                    _demandeSelectionnee = null;
                    _tabActive           = "historique";
                }
            }
            else
            {
                demande.Statut = ancien;
                AfficherToast($"Erreur : {reponse.Message}", "toast-error");
            }
        }

        private async Task RefuserDemande(int demandeId)
        {
            if (string.IsNullOrWhiteSpace(_motifRefus)) return;

            var demande = _demandes.FirstOrDefault(d => d.Id == demandeId);
            if (demande == null) return;

            var reponse = await DemandeAchatSvc.ChangerStatutAsync(demandeId, "refuse", _nomUtilisateur, _motifRefus.Trim());

            if (reponse.Succes)
            {
                demande.Statut     = "refuse";
                demande.MotifRefus = _motifRefus.Trim();
                AfficherToast("Demande refusée et archivée.", "toast-error");
                _motifRefus          = string.Empty;
                _demandeSelectionnee = null;
                _tabActive           = "historique";
            }
            else
            {
                AfficherToast($"Erreur : {reponse.Message}", "toast-error");
            }
        }

        private async Task PrevisualiserOffre(OffreVm o)
        {
            _offrePreview        = o;
            _offrePreview.PdfUrl = null;
            StateHasChanged();

            try
            {
                var url    = $"api/demandes/{o.IdDemande}/offres/{o.Id}/pdf";
                var bytes  = await _http.GetByteArrayAsync(url);
                var base64 = Convert.ToBase64String(bytes);
                _offrePreview.PdfUrl = $"data:application/pdf;base64,{base64}";
            }
            catch (Exception ex)
            {
                AfficherToast($"Erreur aperçu : {ex.Message}", "toast-error");
            }
            StateHasChanged();
        }

        private void FermerPreview() => _offrePreview = null;
        private void ToggleSidebar() => _sidebarOpen  = !_sidebarOpen;

        private static DemandeVm MapDtoVersVm(DemandeAchatDto dto)
        {
            var parts     = dto.DemandeurNom.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var initiales = parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : dto.DemandeurNom.Length > 0
                    ? dto.DemandeurNom[..Math.Min(2, dto.DemandeurNom.Length)].ToUpper()
                    : "??";

            return new DemandeVm
            {
                Id           = dto.IdDemande,
                Reference    = dto.Reference,
                NomProduit   = dto.NomProduit,
                Quantite     = dto.Quantite,
                Description  = dto.Description,
                Statut       = dto.Statut,
                DateCreation = dto.DateCreation,
                DemandeurNom = dto.DemandeurNom,
                Initiales    = initiales,
                MotifRefus   = dto.MotifRefus,
                Lignes       = dto.Lignes.Select(l => new LigneVm
                {
                    IdLigne     = l.IdLigne,
                    Reference   = l.Reference,
                    NomProduit  = l.NomProduit,
                    Quantite    = l.Quantite,
                    Description = l.Description
                }).ToList(),
                Offres = dto.Offres.Select(o => new OffreVm
                {
                    Id         = o.IdOffre,
                    IdDemande  = dto.IdDemande,
                    NomFichier = o.NomFichier,
                    Taille     = o.Taille,
                    EstChoisie = o.EstChoisie
                }).ToList(),
                VuParAchatLe = dto.VuParAchatLe
            };
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

        private static string LibelleStatut(string s) => s switch
        {
            "en_attente"          => "EN ATTENTE",
            "en_cours_traitement" => "EN COURS",
            "commande"            => "COMMANDÉE",
            "traite"              => "TRAITÉE",
            "refuse"              => "REFUSÉE",
            _                     => s.ToUpper()
        };

        private static string FormatDateCarte(DateTime d)
        {
            var diff = DateTime.Now - d; 
            if (diff.TotalSeconds < 60)  return "À l'instant";
            if (diff.TotalMinutes < 60)  return $"Il y a {(int)diff.TotalMinutes} min";
            if (diff.TotalHours   < 24)  return $"Il y a {(int)diff.TotalHours} h";
            if (diff.TotalDays    <  2)  return "Hier";
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

        private async Task ExporterExcel()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Référence;Titre demande;Statut;Demandeur;Date création;Motif refus;Ref Matériel;Produit;Quantité;Description");

                foreach (var d in _demandes)
                {
                    if (d.Lignes.Any())
                    {
                        foreach (var l in d.Lignes)
                        {
                            sb.AppendLine(
                                $"{d.Reference};{d.NomProduit.Replace(";",",")};{LibelleStatut(d.Statut)};" +
                                $"{d.DemandeurNom.Replace(";",",")};{d.DateCreation:dd/MM/yyyy HH:mm};" +
                                $"{(d.MotifRefus ?? "").Replace(";",",")};" +
                                $"{l.Reference};{l.NomProduit.Replace(";",",")};{l.Quantite};{(l.Description ?? "").Replace(";",",")}");
                        }
                    }
                    else
                    {
                        sb.AppendLine(
                            $"{d.Reference};{d.NomProduit.Replace(";",",")};{LibelleStatut(d.Statut)};" +
                            $"{d.DemandeurNom.Replace(";",",")};{d.DateCreation.ToLocalTime():dd/MM/yyyy HH:mm};" +
                            $"{(d.MotifRefus ?? "").Replace(";",",")};;;;");
                    }
                }

                var bytes  = System.Text.Encoding.UTF8.GetPreamble()
                             .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                var base64 = Convert.ToBase64String(bytes);
                var nom    = $"demandes-achat-{DateTime.Now:yyyyMMdd-HHmm}.csv";

                await JS.InvokeVoidAsync("eval", $@"
                    (function(){{
                        var a = document.createElement('a');
                        a.href = 'data:text/csv;base64,{base64}';
                        a.download = '{nom}';
                        document.body.appendChild(a); a.click(); document.body.removeChild(a);
                    }})();
                ");
                AfficherToast("Export Excel téléchargé.", "toast-success");
            }
            catch (Exception ex) { AfficherToast($"Erreur export : {ex.Message}", "toast-error"); }
        }

        private async Task ExporterPdf()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.Append(@"<html><head><meta charset='utf-8'/>
                    <style>body{font-family:Arial,sans-serif;font-size:12px;margin:20px}
                    h2{font-size:16px;margin-bottom:10px}p{font-size:11px;color:#666;margin-bottom:14px}
                    table{border-collapse:collapse;width:100%}th{background:#1e293b;color:#fff;padding:8px 10px;text-align:left;font-size:11px}
                    td{padding:6px 10px;border-bottom:1px solid #e2e8f0;font-size:11px}
                    tr:nth-child(even) td{background:#f8fafc}
                    .en_attente{color:#f59e0b;font-weight:600}.en_cours_traitement{color:#0891b2;font-weight:600}
                    .commande{color:#3b82f6;font-weight:600}.traite{color:#10b981;font-weight:600}.refuse{color:#ef4444;font-weight:600}
                    </style></head><body>");
                sb.Append($"<h2>Liste des demandes d'achat</h2>");
                sb.Append($"<p>Exporté le {DateTime.Now:dd/MM/yyyy à HH:mm} — {_demandes.Count} demande(s)</p>");
                sb.Append("<table><thead><tr><th>Référence</th><th>Titre</th><th>Statut</th><th>Demandeur</th><th>Date</th><th>Matériels</th></tr></thead><tbody>");

                foreach (var d in _demandes)
                {
                    var materiels = d.Lignes.Any()
                        ? string.Join("<br/>", d.Lignes.Select(l => $"{l.NomProduit} ×{l.Quantite}"))
                        : d.NomProduit;

                    sb.Append($"<tr>" +
                              $"<td>{d.Reference}</td><td>{d.NomProduit}</td>" +
                              $"<td class='{d.Statut}'>{LibelleStatut(d.Statut)}</td>" +
                              $"<td>{d.DemandeurNom}</td>" +
                              $"<td>{d.DateCreation:dd/MM/yyyy}</td>" +
                              $"<td>{materiels}</td></tr>");
                }
                sb.Append("</tbody></table></body></html>");

                var html = sb.ToString().Replace("'", "\\'").Replace("\r\n", "").Replace("\n", "");
                await JS.InvokeVoidAsync("eval", $@"
                    (function(){{
                        var w = window.open('','_blank','width=900,height=700');
                        w.document.write('{html}'); w.document.close();
                        setTimeout(function(){{ w.print(); }}, 400);
                    }})();
                ");
                AfficherToast("Fenêtre d'impression ouverte.", "toast-success");
            }
            catch (Exception ex) { AfficherToast($"Erreur export : {ex.Message}", "toast-error"); }
        }
    }
}
