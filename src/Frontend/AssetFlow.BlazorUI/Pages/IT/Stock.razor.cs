using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class Stock
    {
        [Inject] private StockClientService   Svc          { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage  { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private List<MaterielDto> Tous             { get; set; } = new();
        private List<MaterielDto> MaterielsFiltres { get; set; } = new();
        private List<string>      Categories       { get; set; } = new();
        private MaterielStatsViewModel Stats        { get; set; } = new();

        private string Search          { get; set; } = string.Empty;
        private string CategorieFiltre { get; set; } = string.Empty;
        private string UserName        { get; set; } = "IT";
        private bool   Loading         { get; set; } = true;
        private bool   _menuOpen                    = false;

        // Pagination
        private int Page      { get; set; } = 1;
        private int PageSize  { get; set; } = 8;
        private int TotalFiltres => _tousFiltrés.Count;
        private int TotalPages   => Math.Max(1, (int)Math.Ceiling(TotalFiltres / (double)PageSize));
        private List<MaterielDto> _tousFiltrés = new();

        // Modal seuil
        private MaterielDto? MaterielSeuil   { get; set; }
        private int          SeuilMin        { get; set; }
        private string       SeuilErrorMsg   { get; set; } = string.Empty;
        private bool         IsSaving        { get; set; } = false;

        private System.Timers.Timer? _debounce;
        private string      _roleUtilisateur = "Service IT";
        private bool _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        protected override async Task OnInitializedAsync()
        {
            UserName = await LocalStorage.GetItemAsync<string>("user_name") ?? "IT";
            _roleUtilisateur = await LocalStorage.GetItemAsync<string>("user_role") ?? "IT";
            await ChargerMateriels();
        }

        private async Task ChargerMateriels()
        {
            Loading = true; StateHasChanged();
            Tous       = await Svc.GetAllAsync();
            Categories = Tous.Select(m => m.Categorie).Distinct().OrderBy(c => c).ToList();
            Stats      = new MaterielStatsViewModel
            {
                TotalArticles   = Tous.Count,
                EnStock         = Tous.Count(m => m.QuantiteStock > m.QuantiteMin),
                AlerteSeuil     = Tous.Count(m => m.QuantiteStock <= m.QuantiteMin && m.QuantiteStock > 0),
                RuptureCritique = Tous.Count(m => m.QuantiteStock == 0)
            };
            AppliquerFiltres();
            Loading = false; StateHasChanged();
        }

        private void AppliquerFiltres()
        {
            _tousFiltrés = Tous.Where(m =>
            {
                var matchSearch = string.IsNullOrWhiteSpace(Search) ||
                    m.Designation.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                    m.Reference.Contains(Search, StringComparison.OrdinalIgnoreCase);
                var matchCat = string.IsNullOrWhiteSpace(CategorieFiltre) ||
                    m.Categorie == CategorieFiltre;
                return matchSearch && matchCat;
            }).ToList();

            Page = 1;
            MaterielsFiltres = _tousFiltrés.Skip((Page - 1) * PageSize).Take(PageSize).ToList();
            StateHasChanged();
        }

        private void ChangerPage(int p)
        {
            Page = Math.Clamp(p, 1, TotalPages);
            MaterielsFiltres = _tousFiltrés.Skip((Page - 1) * PageSize).Take(PageSize).ToList();
            StateHasChanged();
        }

        private void OnSearchInput(ChangeEventArgs e)
        {
            Search = e.Value?.ToString() ?? string.Empty;
            _debounce?.Stop();
            _debounce = new System.Timers.Timer(300);
            _debounce.Elapsed += (_, _) => { _debounce?.Stop(); InvokeAsync(AppliquerFiltres); };
            _debounce.AutoReset = false; _debounce.Start();
        }

        // ── Modal seuil ──
        private void OuvrirSeuil(MaterielDto mat)
        {
            MaterielSeuil = mat;
            SeuilMin      = mat.QuantiteMin;
            SeuilErrorMsg = string.Empty;
        }

        private void FermerSeuil() { MaterielSeuil = null; SeuilErrorMsg = string.Empty; }

        private async Task SauvegarderSeuil()
        {
            if (MaterielSeuil == null) return;
            if (SeuilMin < 0)
            { SeuilErrorMsg = "Le seuil doit être positif."; return; }

            IsSaving = true; StateHasChanged();
            var ok = await Svc.UpdateSeuilAsync(MaterielSeuil.Id, SeuilMin);
            IsSaving = false;

            if (ok)
            {
                var mat = Tous.FirstOrDefault(m => m.Id == MaterielSeuil.Id);
                if (mat != null) mat.QuantiteMin = SeuilMin;
                AppliquerFiltres();
                FermerSeuil();
                await ChargerMateriels();
            }
            else { SeuilErrorMsg = "Erreur lors de la sauvegarde."; StateHasChanged(); }
        }

        // ── Helpers ──
        private (string label, string css) GetStatut(MaterielDto m)
        {
            if (m.QuantiteStock == 0)                            return ("RUPTURE",    "rupture");
            if (m.QuantiteStock <= m.QuantiteMin / 2)            return ("CRITIQUE",   "critique");
            if (m.QuantiteStock <= m.QuantiteMin)                return ("ALERTE",     "alerte");
            return ("DISPONIBLE", "disponible");
        }

        private int GetFillPct(MaterielDto m)
        {
            if (m.QuantiteMin == 0) return 100;
            return Math.Min(100, (int)((double)m.QuantiteStock / (m.QuantiteMin * 3) * 100));
        }

        private int GetZonePct(int from, int to)
        {
            var total = Math.Max(1, to);
            return Math.Min(100, Math.Max(0, (int)((double)(to - from) / total * 100)));
        }

        private string GetInitials()
        {
            var p = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (p.Length >= 2) return $"{p[0][0]}{p[1][0]}".ToUpper();
            if (p.Length == 1 && p[0].Length >= 2) return p[0][..2].ToUpper();
            return "IT";
        }
        private async Task ExporterExcel()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Référence;Désignation;Catégorie;Quantité;Seuil Min;Statut");
                foreach (var m in MaterielsFiltres)
                {
                    var statut = GetStatut(m).label;
                    sb.AppendLine($"{m.Reference};{m.Designation};{m.Categorie};{m.QuantiteStock};{m.QuantiteMin};{statut}");
                }
                var bytes    = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                var b64      = Convert.ToBase64String(bytes);
                var fileName = $"stocks_{DateTime.Now:yyyyMMdd_HHmm}.csv";
                await JS.InvokeVoidAsync("eval", $@"(function(){{var a=document.createElement('a');a.href='data:text/csv;base64,{b64}';a.download='{fileName}';document.body.appendChild(a);a.click();document.body.removeChild(a);}})();");
            }
            catch (Exception ex) { Console.WriteLine($"Erreur export Excel : {ex.Message}"); }
        }

        private async Task ExporterPdf()
        {
            try
            {
                var rows = new System.Text.StringBuilder();
                foreach (var m in MaterielsFiltres)
                {
                    var statut = GetStatut(m).label;
                    rows.AppendLine($"<tr><td>{m.Reference}</td><td>{m.Designation}</td><td>{m.Categorie}</td><td>{m.QuantiteStock}</td><td>{m.QuantiteMin}</td><td>{statut}</td></tr>");
                }
                var html = $@"<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Stocks</title><style>body{{font-family:Arial;font-size:11px;margin:20px}}table{{width:100%;border-collapse:collapse}}th{{background:#136dec;color:#fff;padding:7px 8px;font-size:10px;text-transform:uppercase}}td{{padding:6px 8px;border-bottom:1px solid #eee;font-size:10px}}tr:nth-child(even){{background:#f8fafc}}</style></head><body><h2>Consultation des Stocks</h2><p>Exporté le {DateTime.Now:dd/MM/yyyy HH:mm}</p><table><thead><tr><th>Référence</th><th>Désignation</th><th>Catégorie</th><th>Quantité</th><th>Seuil Min</th><th>Statut</th></tr></thead><tbody>{rows}</tbody></table></body></html>";
                await JS.InvokeVoidAsync("eval", $@"(function(){{var w=window.open('','_blank','width=900,height=700');w.document.write({System.Text.Json.JsonSerializer.Serialize(html)});w.document.close();w.focus();setTimeout(function(){{w.print();}},400);}})();");
            }
            catch (Exception ex) { Console.WriteLine($"Erreur export PDF : {ex.Message}"); }
        }
    }

    public class MaterielStatsViewModel
    {
        public int TotalArticles   { get; set; }
        public int EnStock         { get; set; }
        public int AlerteSeuil     { get; set; }
        public int RuptureCritique { get; set; }
    }
}