using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Blazored.LocalStorage;
using AssetFlow.BlazorUI.DTOs;

namespace AssetFlow.BlazorUI.Components
{
    public partial class AchatSidebar : ComponentBase, IAsyncDisposable
    {
        [Inject] private IJSRuntime            JS             { get; set; } = default!;
        [Inject] private DemandeAchatService   DemandeAchatSvc { get; set; } = default!;
        [Inject] private UnreadMessagesService UnreadSvc      { get; set; } = default!;
        [Inject] private MessagerieService     MsgSvc         { get; set; } = default!;
        [Inject] private ILocalStorageService  LocalStorage   { get; set; } = default!;
        [Inject] private HttpClient            Http           { get; set; } = default!;

        [Parameter] public string ActivePage { get; set; } = string.Empty;
        [Parameter] public bool   ForceOpen  { get; set; } = false;

        // ── Demandes d'achat non vues ──
        private int    _nombreNonVus   = 0;

        // ── Messagerie IT non lus ──
        private int    _unreadMessages => UnreadSvc.UnreadCount;

        private bool   _drawerOpen      = false;
        private string _nomUtilisateur  = "Agent Achat";
        private string _roleUtilisateur = "Service Achat";
        private string _initiales       = "AA";
        private int    _currentUserId   = 0;

        // Hub pour les demandes d'achat (dashboardhub)
        private HubConnection? _hubDashboard;
        // Hub pour la messagerie (chathub)
        private HubConnection? _hubChat;

        protected override async Task OnInitializedAsync()
        {
            // ── Infos utilisateur ──
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

            // ── Lecture userId depuis LocalStorage ──
            _currentUserId = await LocalStorage.GetItemAsync<int>("user_id");

            // ── Compteurs initiaux ──
            _nombreNonVus = await DemandeAchatSvc.GetCountNonVusAsync();
            await RefreshUnreadMessagesAsync();

            // ── Abonnement au service singleton (changements déclenchés par MessagerieIT) ──
            UnreadSvc.OnChanged += OnUnreadChanged;

            // ── Connexions SignalR ──
            await ConnecterDashboardHubAsync();
            await ConnecterChatHubAsync();
        }

        protected override void OnParametersSet()
        {
            if (ForceOpen) _drawerOpen = true;
        }

        // ── Chargement initial du compteur de messages non lus ────────────────
        private async Task RefreshUnreadMessagesAsync()
        {
            try
            {
                var summaries = await MsgSvc.GetConversationsAsync(_currentUserId);
                var total = summaries.Sum(s => s.UnreadCount);
                UnreadSvc.Set(total);
            }
            catch { }
        }

        // ── SignalR : hub des demandes d'achat ────────────────────────────────
        private async Task ConnecterDashboardHubAsync()
        {
            _hubDashboard = new HubConnectionBuilder()
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

            _hubDashboard.Reconnected += async _ =>
            {
                try { await _hubDashboard.InvokeAsync("JoinDashboard"); } catch { }
                await InvokeAsync(async () =>
                {
                    try { _nombreNonVus = await DemandeAchatSvc.GetCountNonVusAsync(); }
                    catch { }
                    finally { StateHasChanged(); }
                });
            };

            _hubDashboard.On("DashboardUpdated", async () =>
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
                await _hubDashboard.StartAsync();
                await _hubDashboard.InvokeAsync("JoinDashboard");
            }
            catch { }
        }

        // ── SignalR : hub de messagerie ───────────────────────────────────────
        private async Task ConnecterChatHubAsync()
        {
            try
            {
                var token  = await LocalStorage.GetItemAsync<string>("access_token") ?? "";
                var hubUrl = Http.BaseAddress!.ToString().TrimEnd('/') + "/chathub";

                _hubChat = new HubConnectionBuilder()
                    .WithUrl(hubUrl, opts =>
                        opts.AccessTokenProvider = () => Task.FromResult<string?>(token))
                    .WithAutomaticReconnect()
                    .Build();

                // Nouveau message reçu → recalculer le compteur
                _hubChat.On<ChatMessageDto>("ReceiveMessage", async msg =>
                {
                    await InvokeAsync(async () =>
                    {
                        if (msg.ReceiverId == _currentUserId && msg.SenderId != _currentUserId)
                        {
                            await RefreshUnreadMessagesAsync();
                        }
                    });
                });

                _hubChat.Reconnected += async _ =>
                {
                    await InvokeAsync(async () =>
                    {
                        try
                        {
                            await _hubChat.SendAsync("UserConnected", _currentUserId);
                            await RefreshUnreadMessagesAsync();
                        }
                        catch { }
                    });
                };

                await _hubChat.StartAsync();
                await _hubChat.SendAsync("UserConnected", _currentUserId);
            }
            catch { }
        }

        // ── Callback du service singleton ─────────────────────────────────────
        private void OnUnreadChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        public async ValueTask DisposeAsync()
        {
            UnreadSvc.OnChanged -= OnUnreadChanged;

            if (_hubDashboard is not null)
            {
                try { await _hubDashboard.InvokeAsync("LeaveDashboard"); } catch { }
                await _hubDashboard.DisposeAsync();
            }

            if (_hubChat is not null)
            {
                try { await _hubChat.SendAsync("UserDisconnected", _currentUserId); } catch { }
                await _hubChat.DisposeAsync();
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