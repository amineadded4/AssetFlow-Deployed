using AssetFlow.BlazorUI.DTOs;
using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Pages.Admin
{
    public partial class AdminAuditLog : ComponentBase
    {
        [Inject] private AuditLogService AuditService { get; set; } = default!;
        [Inject] private IJSRuntime            JS           { get; set; } = default!;

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

        private bool HasActiveFilters =>
            FilterDateDebut != new DateTime(DateTime.Now.Year, 1, 1) ||
            FilterDateFin   != DateTime.Today ||
            !string.IsNullOrWhiteSpace(FilterCategorie) ||
            !string.IsNullOrWhiteSpace(FilterAction)    ||
            !string.IsNullOrWhiteSpace(FilterSearch);

        protected override async Task OnInitializedAsync()
            => await LoadLogsAsync();

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
            // Placeholder : à connecter avec un endpoint export backend
            await JS.InvokeVoidAsync("alert", $"Export {format.ToUpper()} — fonctionnalité à brancher sur l'endpoint /api/audit-logs/export");
        }

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
            "Inscription"  => "cat-inscription",
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