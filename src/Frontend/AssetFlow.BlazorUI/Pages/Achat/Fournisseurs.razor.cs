using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using AssetFlow.BlazorUI.DTOs;
using AssetFlow.BlazorUI.Services;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class Fournisseurs : ComponentBase
    {
        [Inject]
        private AssetFlow.BlazorUI.Services.FournisseurService FournisseurSvc { get; set; } = default!;

        [Inject]
        private IJSRuntime JS { get; set; } = default!;
        private bool _estAdmin => _currentUserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        private class FournisseurVm
        {
            public int Id { get; set; }
            public string Nom { get; set; } = string.Empty;
            public string? Telephone { get; set; }
            public string? Adresse { get; set; }
            public string? Mail { get; set; }
            public int CommandesTotales { get; set; }
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
            public DateTime? DerniereCommande { get; set; }
        }

        private List<FournisseurVm> _tousLesFournisseurs  = new();
        private List<FournisseurVm> _fournisseursAffiches = new();
        private bool    _chargement        = true;
        private string  _erreurGlobale     = string.Empty;
        private int     _totalFournisseurs = 0;
        private int     _totalCommandes    = 0;
        private string  _termeRecherche    = string.Empty;
        private bool    _sidebarOpen       = false;
        private bool    _panneauOuvert     = false;
        private bool    _modeModif         = false;
        private bool    _sauvegarde        = false;
        private FormulaireVm               _form    = new();
        private Dictionary<string, string> _erreurs = new();
        private FournisseurVm? _fournisseurASupprimer;
        private string _toastMsg  = string.Empty;
        private string _toastType = "toast-success";

        private string _currentUserName = "Administrateur";
        private string _currentUserRole = "Admin système";

        private string CurrentUserInitials
        {
            get
            {
                var parts = _currentUserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
                if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
                return "AD";
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await ChargerInfosUtilisateur();
            await ChargerFournisseurs();
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        private FournisseurVm? TrouverFournisseur(string? designation)
        {
            if (string.IsNullOrWhiteSpace(designation)) return null;

            var exact = _tousLesFournisseurs.FirstOrDefault(f =>
                f.Nom.Equals(designation, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            var terms = designation.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return _tousLesFournisseurs
                .Select(f => new
                {
                    Fournisseur = f,
                    Score = terms.Count(t => f.Nom.ToLower().Contains(t))
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Fournisseur)
                .FirstOrDefault();
        }

        private void OuvrirDetails(FournisseurVm f)
        {
            _tousLesFournisseurs.ForEach(x => x.Expanded = false);
            f.Expanded = true;
            AppliquerFiltres();
        }

        private void OuvrirFormulaireAjout() => OuvrirFormulaire(null);
        private void OuvrirFormulaireModification(FournisseurVm f) => OuvrirFormulaire(f);

        private async Task ChargerInfosUtilisateur()
        {
            try
            {
                var nom = await JS.InvokeAsync<string?>("eval",
                    "localStorage.getItem('user_name') || localStorage.getItem('userFullName') || localStorage.getItem('currentUserName')");
                var role = await JS.InvokeAsync<string?>("eval",
                    "localStorage.getItem('user_role') || localStorage.getItem('currentUserRole')");

                if (!string.IsNullOrWhiteSpace(nom))  _currentUserName = SupprimerGuillemets(nom);
                if (!string.IsNullOrWhiteSpace(role)) _currentUserRole = SupprimerGuillemets(role);
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

        private async Task ChargerFournisseurs()
        {
            _chargement = true;
            _erreurGlobale = string.Empty;
            try
            {
                var dtos = await FournisseurSvc.GetAllAsync();
                _tousLesFournisseurs = dtos.Select(d => new FournisseurVm
                {
                    Id               = d.IdFournisseur,
                    Nom              = d.Nom,
                    Telephone        = d.Telephone,
                    Adresse          = d.Adresse,
                    Mail             = d.Mail,
                    CommandesTotales = d.CommandesTotales,
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
            _totalCommandes    = _tousLesFournisseurs.Sum(f => f.CommandesTotales);
        }

        private void OnRecherche(ChangeEventArgs e)
        {
            _termeRecherche = e.Value?.ToString() ?? string.Empty;
            AppliquerFiltres();
        }

        private void AppliquerFiltres()
        {
            var q = _tousLesFournisseurs.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(_termeRecherche))
            {
                var t = _termeRecherche.Trim().ToLower();
                q = q.Where(f =>
                    f.Nom.ToLower().Contains(t) ||
                    (f.Telephone != null && f.Telephone.Contains(t)) ||
                    (f.Mail      != null && f.Mail.ToLower().Contains(t)) ||
                    (f.Adresse   != null && f.Adresse.ToLower().Contains(t)));
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
            _modeModif     = vm is not null;
            _panneauOuvert = true;
            _form = vm is not null
                ? new FormulaireVm
                {
                    Id               = vm.Id,
                    Nom              = vm.Nom,
                    Telephone        = vm.Telephone ?? string.Empty,
                    Adresse          = vm.Adresse   ?? string.Empty,
                    Mail             = vm.Mail      ?? string.Empty,
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
            if (_erreurs.Any()) return;

            _sauvegarde = true;
            try
            {
                if (_modeModif)
                {
                    var dto = new ModifierFournisseurDto
                    {
                        IdFournisseur    = _form.Id,
                        Nom              = _form.Nom.Trim(),
                        Telephone        = Vide(_form.Telephone),
                        Adresse          = Vide(_form.Adresse),
                        Mail             = Vide(_form.Mail),
                        DerniereCommande = _form.DerniereCommande
                    };
                    var r = await FournisseurSvc.ModifierAsync(dto);
                    if (r.Succes)
                    {
                        var vm = _tousLesFournisseurs.FirstOrDefault(f => f.Id == _form.Id);
                        if (vm != null)
                        {
                            vm.Nom              = _form.Nom.Trim();
                            vm.Telephone        = Vide(_form.Telephone);
                            vm.Adresse          = Vide(_form.Adresse);
                            vm.Mail             = Vide(_form.Mail);
                            vm.DerniereCommande = _form.DerniereCommande;
                        }
                        RecalculerStats(); AppliquerFiltres(); FermerFormulaire();
                        AfficherToast($"« {_form.Nom} » mis à jour.", "toast-success");
                    }
                    else _erreurGlobale = r.Message;
                }
                else
                {
                    var dto = new CreerFournisseurDto
                    {
                        Nom              = _form.Nom.Trim(),
                        Telephone        = Vide(_form.Telephone),
                        Adresse          = Vide(_form.Adresse),
                        Mail             = Vide(_form.Mail),
                        CommandesTotales = 0,
                        DerniereCommande = _form.DerniereCommande
                    };
                    var r = await FournisseurSvc.AjouterAsync(dto);
                    if (r.Succes)
                    {
                        _tousLesFournisseurs.Insert(0, new FournisseurVm
                        {
                            Id               = r.IdFournisseur ?? 0,
                            Nom              = _form.Nom.Trim(),
                            Telephone        = Vide(_form.Telephone),
                            Adresse          = Vide(_form.Adresse),
                            Mail             = Vide(_form.Mail),
                            DerniereCommande = _form.DerniereCommande
                        });
                        RecalculerStats(); AppliquerFiltres(); FermerFormulaire();
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
            var id  = _fournisseurASupprimer.Id;
            _fournisseurASupprimer = null;
            var r = await FournisseurSvc.SupprimerAsync(id);
            if (r.Succes)
            {
                _tousLesFournisseurs.RemoveAll(f => f.Id == id);
                RecalculerStats(); AppliquerFiltres();
                AfficherToast($"« {nom} » supprimé.", "toast-success");
            }
            else _erreurGlobale = r.Message;
        }

        private void ToggleSidebar() => _sidebarOpen = !_sidebarOpen;

        // ── Export Excel (CSV UTF-8 BOM) ──────────────────────────
        private async Task ExporterExcel()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Nom;Téléphone;Adresse;Email;Commandes totales;Dernière commande");

                foreach (var f in _tousLesFournisseurs)
                {
                    sb.AppendLine(
                        $"{Csv(f.Nom)};" +
                        $"{Csv(f.Telephone ?? "")};" +
                        $"{Csv(f.Adresse   ?? "")};" +
                        $"{Csv(f.Mail      ?? "")};" +
                        $"{f.CommandesTotales};" +
                        $"{(f.DerniereCommande.HasValue ? f.DerniereCommande.Value.ToString("dd/MM/yyyy") : "")}");
                }

                var bytes  = System.Text.Encoding.UTF8.GetPreamble()
                             .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
                             .ToArray();
                var base64 = Convert.ToBase64String(bytes);
                var nom    = $"fournisseurs-{DateTime.Now:yyyyMMdd-HHmm}.csv";

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
                AfficherToast("Export Excel téléchargé.", "toast-success");
            }
            catch (Exception ex)
            {
                AfficherToast($"Erreur export : {ex.Message}", "toast-error");
            }
        }

        // ── Export PDF ────────────────────────────────────────────
        private async Task ExporterPdf()
        {
            try
            {
                var rows = new System.Text.StringBuilder();
                foreach (var f in _tousLesFournisseurs)
                {
                    var contact = !string.IsNullOrEmpty(f.Mail)      ? HE(f.Mail)
                                : !string.IsNullOrEmpty(f.Telephone) ? HE(f.Telephone)
                                : "—";
                    var dc = f.DerniereCommande.HasValue
                        ? f.DerniereCommande.Value.ToString("dd/MM/yyyy") : "—";

                    rows.AppendLine(
                        $"<tr>" +
                        $"<td>{HE(f.Nom)}</td>" +
                        $"<td>{f.CommandesTotales}</td>" +
                        $"<td>{dc}</td>" +
                        $"<td>{contact}</td>" +
                        $"</tr>");
                }

                var html =
                    "<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Fournisseurs</title>" +
                    "<style>" +
                    "body{font-family:Arial,sans-serif;font-size:11px;margin:20px}" +
                    "h2{font-size:15px;margin-bottom:4px}" +
                    "p{font-size:10px;color:#666;margin-bottom:12px}" +
                    "table{width:100%;border-collapse:collapse}" +
                    "th{background:#1e293b;color:#fff;padding:7px 8px;font-size:10px;text-transform:uppercase}" +
                    "td{padding:6px 8px;border-bottom:1px solid #eee;font-size:10px}" +
                    "tr:nth-child(even) td{background:#f8fafc}" +
                    "</style></head><body>" +
                    "<h2>Liste des fournisseurs</h2>" +
                    $"<p>Exporté le {DateTime.Now:dd/MM/yyyy HH:mm} — {_tousLesFournisseurs.Count} fournisseur(s)</p>" +
                    "<table><thead><tr>" +
                    "<th>Nom</th><th>Commandes</th><th>Dernière commande</th><th>Contact</th>" +
                    "</tr></thead><tbody>" +
                    rows +
                    "</tbody></table></body></html>";

                await JS.InvokeVoidAsync("eval",
                    $"(function(){{" +
                    $"var w=window.open('','_blank','width=960,height=720');" +
                    $"w.document.write({System.Text.Json.JsonSerializer.Serialize(html)});" +
                    $"w.document.close();" +
                    $"w.focus();" +
                    $"setTimeout(function(){{w.print();}},400);" +
                    $"}})();");

                AfficherToast("PDF ouvert.", "toast-success");
            }
            catch (Exception ex)
            {
                AfficherToast($"Erreur export : {ex.Message}", "toast-error");
            }
        }

        private async void AfficherToast(string msg, string type)
        {
            _toastMsg = msg; _toastType = type; StateHasChanged();
            await Task.Delay(3500);
            _toastMsg = string.Empty; StateHasChanged();
        }

        private static string? Vide(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

        private static string HE(string v) =>
            v.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

        private static string Csv(string v) =>
            v.Contains(';') || v.Contains('"') || v.Contains('\n')
                ? $"\"{v.Replace("\"", "\"\"")}\"" : v;
    }
}