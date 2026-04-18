using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace AssetFlow.BlazorUI.Components
{
    public partial class AchatSidebar : ComponentBase, IAsyncDisposable
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private DemandeAchatService DemandeAchatSvc { get; set; } = default!;

        [Parameter] public string ActivePage { get; set; } = string.Empty;
        [Parameter] public bool   ForceOpen  { get; set; } = false;

        // ── NombreNonVus géré entièrement en interne (plus de [Parameter]) ──
        private int    _nombreNonVus   = 0;
        private bool   _drawerOpen      = false;
        private string _nomUtilisateur  = "Agent Achat";
        private string _roleUtilisateur = "Service Achat";
        private string _initiales       = "AA";
        private HubConnection? _hubConnection;

        protected override async Task OnInitializedAsync()
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

            _nombreNonVus = await DemandeAchatSvc.GetCountNonVusAsync();
            await ConnecterSignalR();
        }

        private async Task ConnecterSignalR()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5235/dashboardhub", options =>
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

            // Après reconnexion → rejoindre le groupe + resynchroniser le badge
            _hubConnection.Reconnected += async _ =>
            {
                try { await _hubConnection.InvokeAsync("JoinDashboard"); } catch { }
                await InvokeAsync(async () =>
                {
                    try { _nombreNonVus = await DemandeAchatSvc.GetCountNonVusAsync(); }
                    catch { }
                    finally { StateHasChanged(); }
                });
            };

            // Nouvelle demande créée/modifiée → resynchroniser le badge
            _hubConnection.On("DashboardUpdated", async () =>
            {
                await InvokeAsync(async () =>
                {
                    try { _nombreNonVus = await DemandeAchatSvc.GetCountNonVusAsync(); }
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

        protected override void OnParametersSet()
        {
            if (ForceOpen) _drawerOpen = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                try { await _hubConnection.InvokeAsync("LeaveDashboard"); } catch { }
                await _hubConnection.DisposeAsync();
            }
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
    }
}