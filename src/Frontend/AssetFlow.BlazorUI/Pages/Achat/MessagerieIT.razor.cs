using System.Net.Http.Json;
using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    // DTO pour un agent IT vu par l'Achat
    public class ITUserForAchatDto
    {
        public int       Id              { get; set; }
        public string    FullName        { get; set; } = string.Empty;
        public string    Initials        { get; set; } = string.Empty;
        public string?   LastMessage     { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int       UnreadCount     { get; set; }
        public bool      IsOnline        { get; set; }
        public bool      IsTyping        { get; set; }
    }

    public partial class MessagerieIT : IAsyncDisposable
    {
        [Inject] private MessagerieService    MsgSvc       { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;
        [Inject] private HttpClient           Http         { get; set; } = default!;
        [Inject] private IJSRuntime           JS           { get; set; } = default!;

        private string UserName       { get; set; } = "Agent Achat";
        private int    CurrentUserId                = 0;
        private bool   _conversationOpen            = false;
        private bool   _hubConnected                = false;
        private bool   LoadingUsers                 = true;
        private bool   LoadingMessages              = false;

        private string SearchIT    { get; set; } = string.Empty;
        private string NewMessage  { get; set; } = string.Empty;

        private List<ITUserForAchatDto> ITUsers      { get; set; } = new();
        private ITUserForAchatDto?      SelectedUser  { get; set; }
        private List<ChatMessageDto>    Messages     { get; set; } = new();

        private HubConnection?       _hub;
        private System.Timers.Timer? _typingTimer;
        private bool                 _isTyping = false;
        private bool _sidebarOpen = false;
        private void ToggleSidebar() => _sidebarOpen = !_sidebarOpen;
        private string      _roleUtilisateur = "Service Achat";
        private bool _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        private List<ITUserForAchatDto> ITUsersFiltres =>
            (string.IsNullOrWhiteSpace(SearchIT)
                ? ITUsers
                : ITUsers.Where(u => u.FullName.Contains(SearchIT, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(u => u.LastMessageTime ?? DateTime.MinValue)
            .ToList();

        private Dictionary<string, List<ChatMessageDto>> GroupedMessages =>
            Messages.GroupBy(m => GroupDateLabel(m.SentAt))
                    .ToDictionary(g => g.Key, g => g.ToList());

        protected override async Task OnInitializedAsync()
        {
            UserName      = await LocalStorage.GetItemAsync<string>("user_name") ?? "Agent Achat";
            CurrentUserId = await LocalStorage.GetItemAsync<int>("user_id");
            _roleUtilisateur = await LocalStorage.GetItemAsync<string>("user_role");

            await LoadITUsersAsync();
            await ConnectHubAsync();
        }

        // ── Connexion SignalR ─────────────────────────────────────
        private async Task ConnectHubAsync()
        {
            try
            {
                var token  = await LocalStorage.GetItemAsync<string>("access_token") ?? "";
                var hubUrl = Http.BaseAddress!.ToString().TrimEnd('/') + "/chathub";

                _hub = new HubConnectionBuilder()
                    .WithUrl(hubUrl, opts => opts.AccessTokenProvider = () => Task.FromResult<string?>(token))
                    .WithAutomaticReconnect()
                    .Build();

                _hub.On<ChatMessageDto>("ReceiveMessage", async msg =>
                {
                    await InvokeAsync(async () =>
                    {
                        var otherId = msg.SenderId == CurrentUserId ? msg.ReceiverId : msg.SenderId;
                        if (SelectedUser?.Id == otherId)
                        {
                            if (!HasOptimistic(msg))
                            {
                                if (msg.SenderId != CurrentUserId)
                                    Messages.Add(msg);
                                else
                                {
                                    var opt = Messages.LastOrDefault(m => m.SenderId == CurrentUserId && m.Id < 0);
                                    if (opt != null) opt.Id = msg.Id;
                                }
                            }
                            if (msg.SenderId != CurrentUserId)
                                await MarkMessagesAsReadAsync(msg.SenderId);
                        }
                        UpdateUserWithMessage(msg);
                        StateHasChanged();
                        await ScrollToBottomAsync();
                    });
                });

                _hub.On<int, int>("MessagesRead", async (readerId, senderId) =>
                {
                    await InvokeAsync(() =>
                    {
                        foreach (var m in Messages.Where(m => !m.IsRead && m.SenderId == CurrentUserId))
                            m.IsRead = true;
                        StateHasChanged();
                    });
                });

                _hub.On<int, bool>("UserOnlineStatus", async (userId, isOnline) =>
                {
                    await InvokeAsync(() =>
                    {
                        var u = ITUsers.FirstOrDefault(x => x.Id == userId);
                        if (u != null) u.IsOnline = isOnline;
                        if (SelectedUser?.Id == userId) SelectedUser.IsOnline = isOnline;
                        StateHasChanged();
                    });
                });

                _hub.On<int, bool>("UserTyping", async (userId, isTyping) =>
                {
                    await InvokeAsync(() =>
                    {
                        if (SelectedUser?.Id == userId) SelectedUser.IsTyping = isTyping;
                        StateHasChanged();
                    });
                });

                _hub.On<List<int>>("OnlineUsers", async (onlineIds) =>
                {
                    await InvokeAsync(() =>
                    {
                        foreach (var u in ITUsers)
                            u.IsOnline = onlineIds.Contains(u.Id);
                        if (SelectedUser != null)
                            SelectedUser.IsOnline = onlineIds.Contains(SelectedUser.Id);
                        StateHasChanged();
                    });
                });

                _hub.Reconnected  += async _ => { _hubConnected = true;  await InvokeAsync(StateHasChanged); };
                _hub.Reconnecting += async _ => { _hubConnected = false; await InvokeAsync(StateHasChanged); };
                _hub.Closed       += async _ => { _hubConnected = false; await InvokeAsync(StateHasChanged); };

                await _hub.StartAsync();
                _hubConnected = true;
                await _hub.SendAsync("UserConnected", CurrentUserId);
                await _hub.SendAsync("GetOnlineUsers");
                StateHasChanged();
            }
            catch { _hubConnected = false; StateHasChanged(); }
        }

        // ── Chargement des agents IT ──────────────────────────────
        private async Task LoadITUsersAsync()
        {
            LoadingUsers = true;
            StateHasChanged();

            try
            {
                // Réutilise le même endpoint que MessagerieEmploye
                var users = await Http.GetFromJsonAsync<List<ITSimpleDto>>("api/users/it") ?? new();
                ITUsers = users.Select(u => new ITUserForAchatDto
                {
                    Id       = u.Id,
                    FullName = u.FullName,
                    Initials = u.Initials,
                }).ToList();

                var summaries = await MsgSvc.GetConversationsAsync(CurrentUserId);
                foreach (var s in summaries)
                {
                    var u = ITUsers.FirstOrDefault(x => x.Id == s.OtherUserId);
                    if (u != null)
                    {
                        u.LastMessage     = s.LastMessage;
                        u.LastMessageTime = s.LastMessageTime;
                        u.UnreadCount     = s.UnreadCount;
                    }
                }
            }
            catch { }

            LoadingUsers = false;
            StateHasChanged();
        }

        // ── Sélection d'un agent IT ───────────────────────────────
        private async Task SelectUser(ITUserForAchatDto user)
        {
            SelectedUser      = user;
            _conversationOpen = true;
            user.UnreadCount  = 0;
            LoadingMessages   = true;
            StateHasChanged();

            Messages = await MsgSvc.GetHistoryAsync(CurrentUserId, user.Id);

            LoadingMessages = false;
            await MarkMessagesAsReadAsync(user.Id);
            StateHasChanged();
            await ScrollToBottomAsync();
        }

        // ── Envoi message ─────────────────────────────────────────
        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(NewMessage) || SelectedUser == null) return;
            if (_hub?.State != HubConnectionState.Connected) return;

            var content    = NewMessage.Trim();
            var receiverId = SelectedUser.Id;

            var optimistic = new ChatMessageDto
            {
                Id         = -(Messages.Count + 1),
                SenderId   = CurrentUserId,
                ReceiverId = receiverId,
                Content    = content,
                SentAt     = DateTime.Now,
                IsRead     = false
            };
            Messages.Add(optimistic);
            UpdateUserWithMessage(optimistic);
            NewMessage = string.Empty;
            StateHasChanged();
            await ScrollToBottomAsync();

            try { await _hub.SendAsync("SendMessage", CurrentUserId, receiverId, content); }
            catch (Exception ex) { Console.WriteLine($"Erreur envoi: {ex.Message}"); }

            await SendTypingAsync(false);
        }

        private async Task OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !e.ShiftKey) { await SendMessage(); return; }
            if (!_isTyping) { _isTyping = true; await SendTypingAsync(true); }
            _typingTimer?.Stop();
            _typingTimer = new System.Timers.Timer(1500);
            _typingTimer.Elapsed += async (_, _) =>
            {
                _typingTimer?.Stop();
                _isTyping = false;
                await InvokeAsync(() => SendTypingAsync(false));
            };
            _typingTimer.AutoReset = false;
            _typingTimer.Start();
        }

        private async Task SendTypingAsync(bool isTyping)
        {
            if (_hub?.State == HubConnectionState.Connected && SelectedUser != null)
                try { await _hub.SendAsync("Typing", CurrentUserId, SelectedUser.Id, isTyping); } catch { }
        }

        private async Task MarkMessagesAsReadAsync(int senderId)
        {
            foreach (var m in Messages.Where(m => m.SenderId == senderId && !m.IsRead))
                m.IsRead = true;
            if (_hub?.State == HubConnectionState.Connected)
                try { await _hub.SendAsync("MarkRead", CurrentUserId, senderId); } catch { }
        }

        private void UpdateUserWithMessage(ChatMessageDto msg)
        {
            var otherId = msg.SenderId == CurrentUserId ? msg.ReceiverId : msg.SenderId;
            var u = ITUsers.FirstOrDefault(x => x.Id == otherId);
            if (u == null) return;
            u.LastMessage     = msg.Content;
            u.LastMessageTime = msg.SentAt;
            if (msg.SenderId != CurrentUserId && SelectedUser?.Id != otherId)
                u.UnreadCount++;
        }

        private bool HasOptimistic(ChatMessageDto msg) =>
            Messages.Any(m => m.Content == msg.Content && m.SenderId == msg.SenderId
                && Math.Abs((m.SentAt - msg.SentAt).TotalSeconds) < 5 && m.Id != msg.Id);

        private void BackToList() { _conversationOpen = false; SelectedUser = null; }

        private async Task ScrollToBottomAsync()
        {
            await Task.Delay(50);
            try { await JS.InvokeVoidAsync("scrollToBottom", "maiMessagesZone"); } catch { }
        }

        private static string GroupDateLabel(DateTime dt)
        {
            var today = DateTime.Today;
            if (dt.Date == today)                return "Aujourd'hui";
            if (dt.Date == today.AddDays(-1))    return "Hier";
            if ((today - dt.Date).TotalDays < 7) return dt.ToString("dddd", new System.Globalization.CultureInfo("fr-FR"));
            return dt.ToString("dd/MM/yyyy");
        }

        private static string FormatConvTime(DateTime? dt)
        {
            if (!dt.HasValue) return string.Empty;
            var today = DateTime.Today;
            if (dt.Value.Date == today)              return dt.Value.ToString("HH:mm");
            if (dt.Value.Date == today.AddDays(-1))  return "Hier";
            return dt.Value.ToString("dd/MM");
        }

        private string GetInitials()
        {
            var parts = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
            return "AA";
        }

        public async ValueTask DisposeAsync()
        {
            _typingTimer?.Dispose();
            if (_hub != null)
            {
                if (_hub.State == HubConnectionState.Connected)
                    try { await _hub.SendAsync("UserDisconnected", CurrentUserId); } catch { }
                await _hub.DisposeAsync();
            }
        }
    }

    public class ITSimpleDto
    {
        public int    Id       { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
    }
}
