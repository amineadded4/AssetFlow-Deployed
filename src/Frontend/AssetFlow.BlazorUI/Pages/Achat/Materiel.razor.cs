// ============================================================
// Pages/Achat/Materiel.razor.cs — v2
// Code-behind : CRUD, image upload, export PDF/Excel, cascade delete
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class Materiel : ComponentBase
    {
        // ── Injections ────────────────────────────────────────────
        [Inject] private AssetFlow.BlazorUI.Services.MaterielService MaterielSvc { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        // ── VM liste ──────────────────────────────────────────────
        private class MaterielVm
        {
            public int      Id            { get; set; }
            public string   Reference     { get; set; } = string.Empty;
            public string   Designation   { get; set; } = string.Empty;
            public string?  Description   { get; set; }
            public string   Categorie     { get; set; } = string.Empty;
            public int      QuantiteStock { get; set; }
            public int      QuantiteMin   { get; set; }
            public string   Unite         { get; set; } = "pièce";
            public string?  Emplacement   { get; set; }
            public string   Etat          { get; set; } = "Disponible";
            public string?  ImageUrl      { get; set; }
        }

        // ── VM formulaire ─────────────────────────────────────────
        private class FormulaireVm
        {
            public int      Id            { get; set; }
            public string   Reference     { get; set; } = string.Empty;
            public string   Designation   { get; set; } = string.Empty;
            public string?  Description   { get; set; }
            public string   Categorie     { get; set; } = string.Empty;
            public int      QuantiteStock { get; set; }
            public int      QuantiteMin   { get; set; }
            public string   Unite         { get; set; } = "pièce";
            public string?  Emplacement   { get; set; }
            public string   Etat          { get; set; } = "Disponible";
            public string?  ImageUrl      { get; set; }
        }

        // ── État ──────────────────────────────────────────────────
        private List<MaterielVm>           _materiels        = new();
        private List<MaterielVm>           _tousMateriels    = new(); // cache complet
        private MaterielStatsDto           _stats            = new();
        private List<string>               _categories       = new();
        private int                        _totalCount       = 0;
        private bool                       _chargement       = true;
        private string                     _erreur           = string.Empty;
        private string                     _termeRecherche   = string.Empty;
        private string                     _categorieFiltre  = "all";
        private string                     _etatFiltre       = "all";
        private string                     _theme            = "dark";
        private bool                       _sidebarOpen      = false;

        // Formulaire
        private bool                       _panneauOuvert    = false;
        private bool                       _modeModif        = false;
        private bool                       _sauvegarde       = false;
        private FormulaireVm               _form             = new();
        private Dictionary<string, string> _erreurs          = new();
        private string                     _erreurFormulaire = string.Empty;

        // Image
        private string  _imagePreview = string.Empty;
        private string  _imageErreur  = string.Empty;
        private bool    _dragOver     = false;
        private string? _imageBase64  = null;
        private string? _imageMime    = null;

        // Suppression
        private MaterielVm? _aSupprimer = null;

        // Toast
        private string _toastMsg  = string.Empty;
        private string _toastType = "toast-success";

        // User
        private string _currentUserName = "Utilisateur";
        private string _currentUserRole = "Équipe Achat";

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

        // ── Cycle de vie ──────────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            try
            {
                var isDark = await JS.InvokeAsync<bool>("eval",
                    "document.documentElement.classList.contains('dark')");
                _theme = isDark ? "dark" : "light";
            }
            catch { _theme = "dark"; }

            await ChargerInfosUtilisateur();
            await ChargerDonnees();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;
            try
            {
                await JS.InvokeVoidAsync("eval", @"
                    window.__skSetThemeRef = function(ref) {
                        window.__skThemeObs && window.__skThemeObs.disconnect();
                        window.__skThemeObs = new MutationObserver(function() {
                            var isDark = document.documentElement.classList.contains('dark');
                            ref.invokeMethodAsync('OnThemeChanged', isDark);
                        });
                        window.__skThemeObs.observe(document.documentElement, {
                            attributes: true, attributeFilter: ['class']
                        });
                    };
                ");
                var dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("__skSetThemeRef", dotNetRef);
            }
            catch { }
        }

        // ── Chargement ────────────────────────────────────────────
        private async Task ChargerDonnees()
        {
            _chargement = true; _erreur = string.Empty;
            try
            {
                var statsTask    = MaterielSvc.GetStatsAsync();
                var materielsTask = MaterielSvc.GetAllAsync();
                await Task.WhenAll(statsTask, materielsTask);

                _stats      = statsTask.Result ?? new();
                _totalCount = materielsTask.Result.Count;

                _categories = materielsTask.Result
                    .Select(m => m.Categorie)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                _tousMateriels = materielsTask.Result.Select(d => new MaterielVm
                {
                    Id            = d.Id,
                    Reference     = d.Reference,
                    Designation   = d.Designation,
                    Description   = d.Description,
                    Categorie     = d.Categorie,
                    QuantiteStock = d.QuantiteStock,
                    QuantiteMin   = d.QuantiteMin,
                    Unite         = d.Unite,
                    Emplacement   = d.Emplacement,
                    Etat          = d.Etat,
                    ImageUrl      = d.ImageUrl
                }).ToList();

                AppliquerFiltres();
            }
            catch (Exception ex) { _erreur = $"Erreur de chargement : {ex.Message}"; }
            finally { _chargement = false; }
        }

        private void AppliquerFiltres()
        {
            var q = _tousMateriels.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_termeRecherche))
            {
                var t = _termeRecherche.Trim().ToLower();
                q = q.Where(m =>
                    m.Designation.ToLower().Contains(t) ||
                    m.Reference.ToLower().Contains(t)   ||
                    (m.Description?.ToLower().Contains(t) ?? false));
            }
            if (_categorieFiltre != "all")
                q = q.Where(m => m.Categorie.Equals(_categorieFiltre, StringComparison.OrdinalIgnoreCase));
            if (_etatFiltre != "all")
                q = q.Where(m => m.Etat.Equals(_etatFiltre, StringComparison.OrdinalIgnoreCase));

            _materiels = q.ToList();
        }

        // ── Filtres ───────────────────────────────────────────────
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

        private void OnEtatChange(ChangeEventArgs e)
        {
            _etatFiltre = e.Value?.ToString() ?? "all";
            AppliquerFiltres();
        }

        // ── Formulaire ────────────────────────────────────────────
        private void OuvrirFormulaire(MaterielVm? vm)
        {
            _erreurs.Clear();
            _erreurFormulaire = string.Empty;
            _imageErreur      = string.Empty;
            _modeModif        = vm is not null;
            _panneauOuvert    = true;

            if (vm is not null)
            {
                _form = new FormulaireVm
                {
                    Id            = vm.Id,
                    Reference     = vm.Reference,
                    Designation   = vm.Designation,
                    Description   = vm.Description,
                    Categorie     = vm.Categorie,
                    QuantiteStock = vm.QuantiteStock,
                    QuantiteMin   = vm.QuantiteMin,
                    Unite         = vm.Unite,
                    Emplacement   = vm.Emplacement,
                    Etat          = vm.Etat,
                    ImageUrl      = vm.ImageUrl
                };
                _imagePreview = vm.ImageUrl ?? string.Empty;
                _imageBase64  = null;
                _imageMime    = null;
            }
            else
            {
                _form         = new FormulaireVm();
                _imagePreview = string.Empty;
                _imageBase64  = null;
                _imageMime    = null;
            }
        }

        private void FermerFormulaire()
        {
            _panneauOuvert    = false;
            _erreurs.Clear();
            _erreurFormulaire = string.Empty;
            _imagePreview     = string.Empty;
            _imageBase64      = null;
            _imageMime        = null;
        }

        // ── Upload image ─────────────────────────────────────────

        private async Task OnImageSelected(InputFileChangeEventArgs e)
        {
            _imageErreur = string.Empty;
            var file = e.File;
            if (file == null) return;

            if (file.Size > 2 * 1024 * 1024)
            {
                _imageErreur = "L'image ne doit pas dépasser 2 Mo.";
                return;
            }
            if (!new[] { "image/jpeg", "image/png", "image/webp" }.Contains(file.ContentType))
            {
                _imageErreur = "Format non supporté. Utilisez JPG ou PNG.";
                return;
            }

            try
            {
                using var stream = file.OpenReadStream(2 * 1024 * 1024);
                using var ms     = new MemoryStream();
                await stream.CopyToAsync(ms);
                var bytes     = ms.ToArray();
                _imageBase64  = Convert.ToBase64String(bytes);
                _imageMime    = file.ContentType;
                _imagePreview = $"data:{_imageMime};base64,{_imageBase64}";
                _form.ImageUrl = _imagePreview; // stocke en base64 (le backend peut sauvegarder)
            }
            catch (Exception ex) { _imageErreur = $"Erreur lecture image : {ex.Message}"; }
        }

        private void OnDragOver() => _dragOver = true;
        private void OnDragLeave() => _dragOver = false;

        private void SupprimerImage()
        {
            _imagePreview  = string.Empty;
            _imageBase64   = null;
            _imageMime     = null;
            _form.ImageUrl = null;
        }

        // ── Sauvegarde ────────────────────────────────────────────
        private async Task Sauvegarder()
        {
            _erreurs.Clear();
            _erreurFormulaire = string.Empty;

            if (string.IsNullOrWhiteSpace(_form.Designation))
                _erreurs["Designation"] = "La désignation est obligatoire.";
            if (string.IsNullOrWhiteSpace(_form.Reference))
                _erreurs["Reference"]   = "La référence est obligatoire.";
            if (string.IsNullOrWhiteSpace(_form.Categorie))
                _erreurs["Categorie"]   = "La catégorie est obligatoire.";
            if (_erreurs.Any()) return;

            _sauvegarde = true;
            try
            {
                MaterielResultDto result;
                if (_modeModif)
                {
                    result = await MaterielSvc.ModifierAsync(new ModifierMaterielDto
                    {
                        Id            = _form.Id,
                        Reference     = _form.Reference.Trim(),
                        Designation   = _form.Designation.Trim(),
                        Description   = Vide(_form.Description),
                        Categorie     = _form.Categorie.Trim(),
                        QuantiteStock = _form.QuantiteStock,
                        QuantiteMin   = _form.QuantiteMin,
                        Unite         = (_form.Unite ?? "pièce").Trim(),
                        Emplacement   = Vide(_form.Emplacement),
                        Etat          = _form.Etat,
                        ImageUrl      = Vide(_form.ImageUrl)
                    });
                }
                else
                {
                    result = await MaterielSvc.AjouterAsync(new CreerMaterielDto
                    {
                        Reference     = _form.Reference.Trim(),
                        Designation   = _form.Designation.Trim(),
                        Description   = Vide(_form.Description),
                        Categorie     = _form.Categorie.Trim(),
                        QuantiteStock = _form.QuantiteStock,
                        QuantiteMin   = _form.QuantiteMin,
                        Unite         = (_form.Unite ?? "pièce").Trim(),
                        Emplacement   = Vide(_form.Emplacement),
                        Etat          = _form.Etat,
                        ImageUrl      = Vide(_form.ImageUrl)
                    });
                }

                if (result.Succes)
                {
                    FermerFormulaire();
                    AfficherToast(
                        _modeModif ? $"« {_form.Designation} » mis à jour." : $"« {_form.Designation} » ajouté.",
                        "toast-success");
                    await ChargerDonnees();
                }
                else
                {
                    _erreurFormulaire = result.Message;
                }
            }
            catch (Exception ex) { _erreurFormulaire = ex.Message; }
            finally { _sauvegarde = false; }
        }

        // ── Suppression cascade ───────────────────────────────────
        private void DemanderSuppression(MaterielVm vm) => _aSupprimer = vm;
        private void AnnulerSuppression() => _aSupprimer = null;

        private async Task ConfirmerSuppression()
        {
            if (_aSupprimer is null) return;
            var nom = _aSupprimer.Designation;
            var id  = _aSupprimer.Id;
            _aSupprimer = null;

            // Suppression cascade (affectations + incidents) — gérée côté service
            var result = await MaterielSvc.SupprimerAvecCascadeAsync(id);
            if (result.Succes)
            {
                AfficherToast($"« {nom} » supprimé.", "toast-success");
                await ChargerDonnees();
            }
            else
            {
                _erreur = result.Message;
            }
        }

        // ── Export Excel ──────────────────────────────────────────
        private async Task ExporterExcel()
        {
            try
            {
                // Génère un CSV lisible par Excel (séparateur point-virgule pour fr)
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Référence;Désignation;Catégorie;Quantité;Seuil Min;Unité;Emplacement;État");
                foreach (var m in _materiels)
                {
                    sb.AppendLine($"{Csv(m.Reference)};{Csv(m.Designation)};{Csv(m.Categorie)};{m.QuantiteStock};{m.QuantiteMin};{Csv(m.Unite)};{Csv(m.Emplacement ?? "")};{Csv(StatusLabel(m.Etat))}");
                }
                var bytes   = System.Text.Encoding.UTF8.GetPreamble()
                    .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                var b64     = Convert.ToBase64String(bytes);
                var fileName = $"materiels_{DateTime.Now:yyyyMMdd_HHmm}.csv";

                await JS.InvokeVoidAsync("eval", $@"
                    (function(){{
                        var a = document.createElement('a');
                        a.href = 'data:text/csv;base64,{b64}';
                        a.download = '{fileName}';
                        document.body.appendChild(a);
                        a.click();
                        document.body.removeChild(a);
                    }})();
                ");
                AfficherToast("Export Excel téléchargé.", "toast-success");
            }
            catch (Exception ex) { AfficherToast($"Erreur export : {ex.Message}", "toast-error"); }
        }

        // ── Export PDF ────────────────────────────────────────────
        private async Task ExporterPdf()
        {
            try
            {
                // Génère un HTML minimal et déclenche window.print()
                var rows = new System.Text.StringBuilder();
                foreach (var m in _materiels)
                {
                    rows.AppendLine($"<tr><td>{HE(m.Reference)}</td><td>{HE(m.Designation)}</td><td>{HE(m.Categorie)}</td><td>{m.QuantiteStock}</td><td>{m.QuantiteMin}</td><td>{HE(m.Unite)}</td><td>{HE(m.Emplacement ?? "—")}</td><td>{HE(StatusLabel(m.Etat))}</td></tr>");
                }

                var html = $@"<!DOCTYPE html><html><head><meta charset='utf-8'/>
<title>Catalogue Matériels</title>
<style>
  body{{font-family:Arial,sans-serif;font-size:11px;margin:20px;}}
  h1{{font-size:16px;margin-bottom:4px;}}
  p{{color:#666;margin-bottom:12px;font-size:10px;}}
  table{{width:100%;border-collapse:collapse;}}
  th{{background:#136dec;color:#fff;padding:7px 8px;text-align:left;font-size:10px;text-transform:uppercase;letter-spacing:.05em;}}
  td{{padding:6px 8px;border-bottom:1px solid #eee;font-size:10px;}}
  tr:nth-child(even){{background:#f8fafc;}}
  @media print{{@page{{margin:1cm;}}}}
</style></head><body>
<h1>Catalogue Matériels</h1>
<p>Exporté le {DateTime.Now:dd/MM/yyyy à HH:mm} — {_materiels.Count} article(s)</p>
<table><thead><tr>
<th>Référence</th><th>Désignation</th><th>Catégorie</th><th>Qté</th><th>Seuil</th><th>Unité</th><th>Emplacement</th><th>État</th>
</tr></thead><tbody>{rows}</tbody></table>
</body></html>";

                await JS.InvokeVoidAsync("eval", $@"
                    (function(){{
                        var w = window.open('','_blank','width=900,height=700');
                        w.document.write({System.Text.Json.JsonSerializer.Serialize(html)});
                        w.document.close();
                        w.focus();
                        setTimeout(function(){{ w.print(); }}, 400);
                    }})();
                ");
                AfficherToast("PDF ouvert pour impression.", "toast-success");
            }
            catch (Exception ex) { AfficherToast($"Erreur PDF : {ex.Message}", "toast-error"); }
        }

        // ── Thème ─────────────────────────────────────────────────
        [JSInvokable("OnThemeChanged")]
        public void OnThemeChanged(bool isDark)
        {
            _theme = isDark ? "dark" : "light";
            InvokeAsync(StateHasChanged);
        }

        private void ToggleSidebar() => _sidebarOpen = !_sidebarOpen;

        // ── Helpers affichage ─────────────────────────────────────
        private static string StatusClass(string etat) => etat switch
        {
            "Disponible"  => "status-ok",
            "EnRupture"   => "status-rupture",
            "EnCommande"  => "status-commande",
            "HorsService" => "status-hors",
            _             => "status-ok"
        };

        private static string StatusLabel(string etat) => etat switch
        {
            "Disponible"  => "Disponible",
            "EnRupture"   => "Rupture",
            "EnCommande"  => "En commande",
            "HorsService" => "Hors service",
            _             => etat
        };

        private static string CatBadgeClass(string cat) => cat.ToLower() switch
        {
            var c when c.Contains("électro") || c.Contains("electro") => "cat-blue",
            var c when c.Contains("mobil") => "cat-amber",
            var c when c.Contains("fourni") => "cat-slate",
            var c when c.Contains("infor") => "cat-purple",
            var c when c.Contains("périph") || c.Contains("periph") => "cat-teal",
            _ => "cat-default"
        };

        private static string? Vide(string? v) =>
            string.IsNullOrWhiteSpace(v) ? null : v.Trim();

        private static string Csv(string v) =>
            v.Contains(';') || v.Contains('"') || v.Contains('\n')
                ? $"\"{v.Replace("\"", "\"\"")}\"" : v;

        private static string HE(string v) =>
            v.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        private async void AfficherToast(string msg, string type)
        {
            _toastMsg = msg; _toastType = type; StateHasChanged();
            await Task.Delay(3500);
            _toastMsg = string.Empty; StateHasChanged();
        }

        // ── Utilisateur ───────────────────────────────────────────
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
            if (v.Length >= 2 &&
                ((v.StartsWith('"') && v.EndsWith('"')) ||
                 (v.StartsWith('\'') && v.EndsWith('\''))))
                v = v[1..^1].Trim();
            return v;
        }
    }
}