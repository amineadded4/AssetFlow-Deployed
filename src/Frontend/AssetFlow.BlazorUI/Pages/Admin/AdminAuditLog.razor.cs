using AssetFlow.BlazorUI.DTOs;
using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Pages.Admin
{
    public partial class AdminAuditLog : ComponentBase
    {
        [Inject] private AuditLogService AuditService { get; set; } = default!;
        [Inject] private IJSRuntime            JS           { get; set; } = default!;
        [Inject] private HttpClient            Http         { get; set; } = default!;

        // ── État ──
        private AuditLogPagedDto? Result       { get; set; }
        private bool              IsLoading    { get; set; } = true;
        private string            ErrorMessage { get; set; } = string.Empty;

        // ── Filtres ──
        private DateTime? FilterDateDebut { get; set; } = new DateTime(DateTime.Now.Year, 1, 1);
        private DateTime? FilterDateFin   { get; set; } = DateTime.Now;
        private string    FilterCategorie { get; set; } = string.Empty;
        private string    FilterAction    { get; set; } = string.Empty;
        private string    FilterSearch    { get; set; } = string.Empty;

        // ── Pagination ──
        private int CurrentPage { get; set; } = 1;
        private int PageSize    { get; set; } = 50;

        // ── Modal détail ──
        private AuditLogDto? DetailLog { get; set; }

        // ── Debounce ──
        private System.Threading.Timer? _debounceTimer;
        // ── SignalR ──
        private HubConnection? _hubConnection;
        private bool           _hubConnected   = false;
        private bool           _isSilentRefresh = false;
        private DateTime       _lastRefreshed   = DateTime.Now;
        private int            _newEntriesCount = 0;
        private HashSet<int>   _knownIds        = new();

        private bool HasActiveFilters =>
            FilterDateDebut != new DateTime(DateTime.Now.Year, 1, 1) ||
            FilterDateFin   != DateTime.Today ||
            !string.IsNullOrWhiteSpace(FilterCategorie) ||
            !string.IsNullOrWhiteSpace(FilterAction)    ||
            !string.IsNullOrWhiteSpace(FilterSearch);
        
        private bool     _showCleanModal  = false;
        private bool     _cleanLoading    = false;
        private bool     _cleanSuccess    = false;
        private string   _cleanMessage    = string.Empty;
        private DateTime _cleanDate       = DateTime.Today.AddMonths(-3);
        private string   _cleanCategorie  = string.Empty;
        private AuditLogStatsDto? _stats  = null;

        // Ouvrir modal + charger stats
        private async Task OuvrirCleanModal()
        {
            _cleanMessage = string.Empty;
            _stats = await AuditService.GetStatsAsync();
            _showCleanModal = true;
        }

        private async Task SupprimerAvantDate()
        {
            if (!await ConfirmerAction(
                $"Supprimer toutes les entrées avant le {_cleanDate:dd/MM/yyyy} ?")) return;

            _cleanLoading = true;
            var (ok, msg) = await AuditService.SupprimerAvantDateAsync(_cleanDate);
            _cleanSuccess = ok;
            _cleanMessage = msg;
            _cleanLoading = false;

            if (ok) { await LoadLogsAsync(); _stats = await AuditService.GetStatsAsync(); }
        }

        private async Task SupprimerParCategorie()
        {
            // Vérification explicite
            if (string.IsNullOrWhiteSpace(_cleanCategorie))
            {
                _cleanSuccess = false;
                _cleanMessage = "Veuillez sélectionner une catégorie.";
                return;
            }

            Console.WriteLine($"[DEBUG] Catégorie sélectionnée : '{_cleanCategorie}'");

            if (!await ConfirmerAction(
                $"Supprimer toutes les entrées de la catégorie '{_cleanCategorie}' ?")) return;

            _cleanLoading = true;
            _cleanMessage = string.Empty;
            StateHasChanged();

            var (ok, msg) = await AuditService.SupprimerParCategorieAsync(_cleanCategorie);
            _cleanSuccess = ok;
            _cleanMessage = msg;
            _cleanLoading = false;

            if (ok) { await LoadLogsAsync(); _stats = await AuditService.GetStatsAsync(); }
            StateHasChanged();
        }

        private async Task SupprimerTout()
        {
            if (!await ConfirmerAction(
                "ATTENTION : Supprimer TOUTES les entrées du journal ? Cette action est irréversible !")) return;

            _cleanLoading = true;
            var (ok, msg) = await AuditService.SupprimerToutAsync();
            _cleanSuccess = ok;
            _cleanMessage = msg;
            _cleanLoading = false;

            if (ok) { await LoadLogsAsync(); _stats = await AuditService.GetStatsAsync(); }
        }

        private async Task<bool> ConfirmerAction(string message)
            => await JS.InvokeAsync<bool>("confirm", message);

        protected override async Task OnInitializedAsync()
        {
            await LoadLogsAsync();
            await ConnecterSignalR();
        }
        // ── SignalR ──
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
 
            _hubConnection.Reconnected += async _ =>
            {
                try { await _hubConnection.InvokeAsync("JoinDashboard"); } catch { }
                await InvokeAsync(async () =>
                {
                    try { await LoadLogsAsync(); }
                    catch { }
                    finally
                    {
                        _hubConnected = true;
                        StateHasChanged();
                    }
                });
            };
 
            _hubConnection.Closed += _ =>
            {
                _hubConnected = false;
                InvokeAsync(StateHasChanged);
                return Task.CompletedTask;
            };
 
            // ← Écoute les mises à jour du dashboard (même event que AdminProjects)
            _hubConnection.On("DashboardUpdated", async () =>
            {
                await InvokeAsync(async () =>
                {
                    try   { await SilentRefreshAsync(); }
                    catch { /* silencieux */ }
                    finally { StateHasChanged(); }
                });
            });
 
            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinDashboard");
                _hubConnected = true;
            }
            catch { /* reste statique si SignalR non dispo */ }
 
            StateHasChanged();
        }
 
        // ── Refresh silencieux déclenché par SignalR ──
        private async Task SilentRefreshAsync()
        {
            _isSilentRefresh = true;
            StateHasChanged();
 
            try
            {
                var fresh = await AuditService.GetLogsAsync(
                    dateDebut:  FilterDateDebut,
                    dateFin:    FilterDateFin,
                    action:     FilterAction,
                    categorie:  FilterCategorie,
                    search:     FilterSearch,
                    page:       CurrentPage,
                    pageSize:   PageSize);
 
                if (fresh != null)
                {
                    if (_knownIds.Count > 0)
                        _newEntriesCount = fresh.Items.Count(i => !_knownIds.Contains(i.Id));
 
                    _knownIds      = fresh.Items.Select(i => i.Id).ToHashSet();
                    Result         = fresh;
                    _lastRefreshed = DateTime.Now;
                }
            }
            catch { /* silencieux */ }
            finally
            {
                _isSilentRefresh = false;
            }
        }
        public async ValueTask DisposeAsync()
        {
            _debounceTimer?.Dispose();
            if (_hubConnection is not null)
            {
                try { await _hubConnection.InvokeAsync("LeaveDashboard"); } catch { }
                await _hubConnection.DisposeAsync();
            }
        }

        private async Task LoadLogsAsync()
        {
            IsLoading    = true;
            ErrorMessage = string.Empty;
            StateHasChanged();

            try
            {
                Result = await AuditService.GetLogsAsync(
                    dateDebut:   FilterDateDebut,
                    dateFin:     FilterDateFin,
                    action:      FilterAction,
                    categorie:   FilterCategorie,
                    search:      FilterSearch,
                    page:        CurrentPage,
                    pageSize:    PageSize);

                if (Result == null)
                    ErrorMessage = "Impossible de charger le journal d'audit.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private void OnFilterChanged()
        {
            CurrentPage = 1;
            _ = LoadLogsAsync();
        }

        private void OnSearchInput(ChangeEventArgs e)
        {
            FilterSearch = e.Value?.ToString() ?? string.Empty;
            _debounceTimer?.Dispose();
            _debounceTimer = new System.Threading.Timer(_ =>
            {
                CurrentPage = 1;
                InvokeAsync(LoadLogsAsync);
            }, null, 450, System.Threading.Timeout.Infinite);
        }

        private void ClearSearch()
        {
            FilterSearch = string.Empty;
            CurrentPage  = 1;
            _ = LoadLogsAsync();
        }

        private void ResetFilters()
        {
            FilterDateDebut  = new DateTime(DateTime.Now.Year, 1, 1);
            FilterDateFin    = DateTime.Now;
            FilterCategorie  = string.Empty;
            FilterAction     = string.Empty;
            FilterSearch     = string.Empty;
            CurrentPage      = 1;
            _ = LoadLogsAsync();
        }

        private async Task GoToPage(int page)
        {
            if (Result == null) return;
            if (page < 1 || page > Result.TotalPages) return;
            CurrentPage = page;
            await LoadLogsAsync();
        }

        private async Task OnPageSizeChanged()
        {
            CurrentPage = 1;
            await LoadLogsAsync();
        }

        private void OpenDetail(AuditLogDto log)  => DetailLog = log;
        private void CloseDetail()                => DetailLog = null;

        private async Task ExportAsync(string format)
        {
            if (Result == null || !Result.Items.Any()) return;

            if (format == "excel") await ExporterExcel();
            else if (format == "pdf") await ExporterPdf();
        }

        private async Task ExporterExcel()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Date;Utilisateur;Email;Action;Catégorie;Entité;Détails");

                foreach (var log in Result!.Items)
                {
                    sb.AppendLine($"{Csv(log.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"))};{Csv(log.Utilisateur)};{Csv(log.Email)};{Csv(log.Action)};{Csv(log.Categorie)};{Csv(log.Entite)};{Csv(log.Details ?? "")}");
                }

                var bytes = System.Text.Encoding.UTF8.GetPreamble()
                            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                var b64   = Convert.ToBase64String(bytes);
                var fn    = $"audit_logs_{DateTime.Now:yyyyMMdd_HHmm}.csv";

                await JS.InvokeVoidAsync("eval",
                    $@"(function(){{var a=document.createElement('a');a.href='data:text/csv;base64,{b64}';a.download='{fn}';document.body.appendChild(a);a.click();document.body.removeChild(a);}})();");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur export : {ex.Message}";
            }
        }

        private async Task ExporterPdf()
        {
            try
            {
                var rows = new System.Text.StringBuilder();
                foreach (var log in Result!.Items)
                {
                    rows.AppendLine($@"
                        <tr>
                            <td>{HE(log.Timestamp.ToString("dd/MM/yyyy HH:mm"))}</td>
                            <td>{HE(log.Utilisateur)}<br/><small style='color:#888'>{HE(log.Email)}</small></td>
                            <td><span class='badge action'>{HE(log.Action)}</span></td>
                            <td><span class='badge cat'>{HE(log.Categorie)}</span></td>
                            <td>{HE(log.Entite)}</td>
                            <td>{HE(log.Details ?? "—")}</td>
                        </tr>");
                }

                var html = $@"<!DOCTYPE html>
        <html><head><meta charset='utf-8'/>
        <title>Journal d'Audit</title>
        <style>
        body{{font-family:Arial;font-size:11px;margin:20px}}
        h2{{color:#136dec}}
        table{{width:100%;border-collapse:collapse}}
        th{{background:#136dec;color:#fff;padding:7px 8px;font-size:10px;text-transform:uppercase;text-align:left}}
        td{{padding:6px 8px;border-bottom:1px solid #eee;font-size:10px;vertical-align:top}}
        tr:nth-child(even){{background:#f8fafc}}
        .badge{{padding:2px 7px;border-radius:4px;font-size:9px;font-weight:bold}}
        .action{{background:#dbeafe;color:#1d4ed8}}
        .cat{{background:#f3e8ff;color:#7c3aed}}
        small{{font-size:9px}}
        </style>
        </head>
        <body>
        <h2>Journal d'Audit — AssetFlow</h2>
        <p style='color:#888'>Exporté le {DateTime.Now:dd/MM/yyyy HH:mm} · {Result!.Items.Count} entrées</p>
        <table>
        <thead>
            <tr>
            <th>Date</th><th>Utilisateur</th><th>Action</th>
            <th>Catégorie</th><th>Entité</th><th>Détails</th>
            </tr>
        </thead>
        <tbody>{rows}</tbody>
        </table>
        </body></html>";

                await JS.InvokeVoidAsync("eval",
                    $@"(function(){{var w=window.open('','_blank','width=1100,height=750');w.document.write({System.Text.Json.JsonSerializer.Serialize(html)});w.document.close();w.focus();setTimeout(function(){{w.print();}},500);}})();");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur export : {ex.Message}";
            }
        }

        // ── Helpers ──
        private static string Csv(string v)
            => v.Contains(';') || v.Contains('"') || v.Contains('\n')
                ? $"\"{v.Replace("\"", "\"\"")}\"" : v;

        private static string HE(string v)
            => System.Net.WebUtility.HtmlEncode(v);

        // ── Pagination helpers ──
        private IEnumerable<int> GetPageNumbers()
        {
            if (Result == null) yield break;
            int total = Result.TotalPages;
            int cur   = CurrentPage;

            yield return 1;
            if (cur > 4) yield return -1; // dots

            for (int p = Math.Max(2, cur - 2); p <= Math.Min(total - 1, cur + 2); p++)
                yield return p;

            if (cur < total - 3) yield return -1; // dots
            if (total > 1) yield return total;
        }

        // ── UI helpers ──
        private static string GetInitials(string name)
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : name[..Math.Min(2, name.Length)].ToUpper();
        }

        private static string GetAvatarColor(string name)
        {
            var colors = new[]
            {
                "#136dec", "#7c3aed", "#059669", "#d97706",
                "#dc2626", "#0891b2", "#c026d3", "#65a30d"
            };
            var idx = Math.Abs(name.GetHashCode()) % colors.Length;
            return colors[idx];
        }

        private static string GetActionClass(string action) => action switch
        {
            "CREATION"       => "action-creation",
            "MODIFICATION"   => "action-modification",
            "SUPPRESSION"    => "action-suppression",
            "CONNEXION"      => "action-connexion",
            "INSCRIPTION"    => "action-inscription",
            "AFFECTATION"    => "action-affectation",
            "REVOCATION"     => "action-revocation",
            "CHANGEMENT ÉTAT"=> "action-changement",
            _                => "action-default"
        };

        private static string GetCatClass(string cat) => cat switch
        {
            "Authentification"  => "cat-inscription",
            "Matériel"     => "cat-materiel",
            "Affectation"  => "cat-affectation",
            "DemandeAchat" => "cat-demande",
            _              => "cat-default"
        };

        private static string GetCatLabel(string cat) => cat switch
        {
            "DemandeAchat" => "Demande Achat",
            _              => cat
        };
    }
}