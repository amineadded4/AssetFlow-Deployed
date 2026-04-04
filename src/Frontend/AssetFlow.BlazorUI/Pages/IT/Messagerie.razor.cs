using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using AssetFlow.BlazorUI.DTOs;

namespace AssetFlow.BlazorUI.Pages.IT
{
    public partial class Messagerie : IAsyncDisposable
    {
        [Inject] private MessagerieService        MsgSvc       { get; set; } = default!;
        [Inject] private EmployeManagementService EmpSvc       { get; set; } = default!;
        [Inject] private ILocalStorageService     LocalStorage { get; set; } = default!;
        [Inject] private HttpClient               Http         { get; set; } = default!;
        [Inject] private IJSRuntime               JS           { get; set; } = default!;
        [Inject] private VoiceCommandService VoiceSvc { get; set; } = default!;

        private string UserName       { get; set; } = "IT";
        private int    CurrentUserId                = 0;
        private bool   _menuOpen                    = false;
        private bool   _conversationOpen            = false;
        private bool   _hubConnected                = false;
        private bool   LoadingConversations          = true;
        private bool   LoadingMessages               = false;

        private string SearchConv { get; set; } = string.Empty;
        private string NewMessage { get; set; } = string.Empty;

        private List<ConversationDto>  Conversations   { get; set; } = new();
        private ConversationDto?       SelectedConv    { get; set; }
        private List<ChatMessageDto>   CurrentMessages { get; set; } = new();

        private HubConnection?       _hub;
        private System.Timers.Timer? _typingTimer;
        private bool                 _isTyping = false;

        private string RoleFilter { get; set; } = "Tous";
        private string      _roleUtilisateur = "Service IT";
        private bool _estAdmin => _roleUtilisateur.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        private List<ConversationDto> ConversationsFiltrees =>
            Conversations
                .Where(c => string.IsNullOrWhiteSpace(SearchConv)
                        || c.FullName.Contains(SearchConv, StringComparison.OrdinalIgnoreCase))
                .Where(c => RoleFilter == "Tous" || c.Role == RoleFilter)
                .OrderByDescending(c => c.LastMessageTime ?? DateTime.MinValue)
                .ToList();

        private Dictionary<string, List<ChatMessageDto>> GroupedMessages =>
            CurrentMessages
                .GroupBy(m => GroupDateLabel(m.SentAt))
                .ToDictionary(g => g.Key, g => g.ToList());

        protected override async Task OnInitializedAsync()
        {
            VoiceSvc.OnCommand += HandleVoiceCommand;
            UserName = await LocalStorage.GetItemAsync<string>("user_name") ?? "IT User";
            CurrentUserId = await LocalStorage.GetItemAsync<int>("user_id");
            _roleUtilisateur = await LocalStorage.GetItemAsync<string>("user_role") ?? "IT";

            await LoadConversationsAsync();
            await ConnectHubAsync();
        }
        private async Task HandleVoiceCommand(VoiceCommand cmd)
        {
            await InvokeAsync(async () =>
            {
                switch (cmd.Type)
                {
                    case VoiceCommandType.SélectionnerConversation 
                        when !string.IsNullOrWhiteSpace(cmd.Designation):
                    {
                        // Recherche insensible à la casse et partielle
                        var recherche = cmd.Designation.Trim();
                        var user = Conversations.FirstOrDefault(u =>
                            u.FullName.Contains(recherche, StringComparison.OrdinalIgnoreCase));

                        if (user != null)
                            await SelectConversation(user);
                        else
                        {
                            // Tentative de correspondance mot par mot
                            var mots = recherche.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            user = Conversations.FirstOrDefault(u =>
                                mots.All(m => u.FullName.Contains(m, StringComparison.OrdinalIgnoreCase)));

                            if (user != null)
                                await SelectConversation(user);
                        }
                        break;
                    }
                }
                StateHasChanged();
            });
        }

        private async Task ConnectHubAsync()
        {
            try
            {
                var token  = await LocalStorage.GetItemAsync<string>("access_token") ?? "";
                // Pointer vers le backend (Http.BaseAddress = http://localhost:5235/)
                var hubUrl = Http.BaseAddress!.ToString().TrimEnd('/') + "/chathub";

                _hub = new HubConnectionBuilder()
                    .WithUrl(hubUrl, opts =>
                    {
                        opts.AccessTokenProvider = () => Task.FromResult<string?>(token);
                    })
                    .WithAutomaticReconnect()
                    .Build();

                _hub.On<ChatMessageDto>("ReceiveMessage", async msg =>
                {
                    await InvokeAsync(async () =>
                    {
                        var otherId = msg.SenderId == CurrentUserId ? msg.ReceiverId : msg.SenderId;
                        if (SelectedConv?.EmployeId == otherId)
                        {
                            // Éviter les doublons (le message optimiste est déjà affiché)
                            if (!CurrentMessages.Any(m => m.Content == msg.Content && m.SenderId == msg.SenderId
                                && Math.Abs((m.SentAt - msg.SentAt).TotalSeconds) < 5 && m.Id != msg.Id))
                            {
                                if (msg.SenderId != CurrentUserId)
                                    CurrentMessages.Add(msg);
                                else
                                {
                                    // Mettre à jour l'ID réel du message optimiste
                                    var optimistic = CurrentMessages.LastOrDefault(m => m.SenderId == CurrentUserId && m.Id < 1000);
                                    if (optimistic != null) optimistic.Id = msg.Id;
                                }
                            }
                            if (msg.SenderId != CurrentUserId)
                                await MarkMessagesAsReadAsync(msg.SenderId);
                        }
                        UpdateConversationWithMessage(msg);
                        StateHasChanged();
                        await ScrollToBottomAsync();
                    });
                });

                _hub.On<int, int>("MessagesRead", async (readerId, senderId) =>
                {
                    await InvokeAsync(() =>
                    {
                        foreach (var m in CurrentMessages.Where(m => !m.IsRead && m.SenderId == CurrentUserId))
                            m.IsRead = true;
                        StateHasChanged();
                    });
                });

                _hub.On<int, bool>("UserOnlineStatus", async (userId, isOnline) =>
                {
                    await InvokeAsync(() =>
                    {
                        var conv = Conversations.FirstOrDefault(c => c.EmployeId == userId);
                        if (conv != null) conv.IsOnline = isOnline;
                        if (SelectedConv?.EmployeId == userId) SelectedConv.IsOnline = isOnline;
                        StateHasChanged();
                    });
                });

                _hub.On<int, bool>("UserTyping", async (userId, isTyping) =>
                {
                    await InvokeAsync(() =>
                    {
                        if (SelectedConv?.EmployeId == userId) SelectedConv.IsTyping = isTyping;
                        StateHasChanged();
                    });
                });

                _hub.Reconnected  += async _ => { _hubConnected = true;  await InvokeAsync(StateHasChanged); };
                _hub.Reconnecting += async _ => { _hubConnected = false; await InvokeAsync(StateHasChanged); };
                _hub.Closed       += async _ => { _hubConnected = false; await InvokeAsync(StateHasChanged); };

                _hub.On<List<int>>("OnlineUsers", async (onlineUserIds) =>
                {
                    await InvokeAsync(() =>
                    {
                        foreach (var conv in Conversations)
                            conv.IsOnline = onlineUserIds.Contains(conv.EmployeId);
                        if (SelectedConv != null)
                            SelectedConv.IsOnline = onlineUserIds.Contains(SelectedConv.EmployeId);
                        StateHasChanged();
                    });
                });
                await _hub.StartAsync();
                _hubConnected = true;
                // Annoncer la présence avec l'userId explicite
                await _hub.SendAsync("UserConnected", CurrentUserId);
                await _hub.SendAsync("GetOnlineUsers");
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hub connection error: {ex.Message}");
                _hubConnected = false;
                StateHasChanged();
            }
        }

        private async Task LoadConversationsAsync()
        {
            LoadingConversations = true;
            StateHasChanged();

            var employees = await EmpSvc.GetEmployesAsync();
            Conversations = employees.Select(e => new ConversationDto
            {
                EmployeId = e.Id,
                FullName  = e.FullName,
                Initials  = e.Initials,
                Role      = e.Role ?? string.Empty,
            }).ToList();

            var summaries = await MsgSvc.GetConversationsAsync(CurrentUserId);
            foreach (var s in summaries)
            {
                var conv = Conversations.FirstOrDefault(c => c.EmployeId == s.OtherUserId);
                if (conv != null)
                {
                    conv.LastMessage     = s.LastMessage;
                    conv.LastMessageTime = s.LastMessageTime;
                    conv.UnreadCount     = s.UnreadCount;
                }
            }

            LoadingConversations = false;
            StateHasChanged();
        }

        private async Task SelectConversation(ConversationDto conv)
        {
            SelectedConv      = conv;
            _conversationOpen = true;
            conv.UnreadCount  = 0;
            LoadingMessages   = true;
            StateHasChanged();

            CurrentMessages = await MsgSvc.GetHistoryAsync(CurrentUserId, conv.EmployeId);

            LoadingMessages = false;
            await MarkMessagesAsReadAsync(conv.EmployeId);
            StateHasChanged();
            await ScrollToBottomAsync();
        }

        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(NewMessage) || SelectedConv == null) return;
            if (_hub?.State != HubConnectionState.Connected) return;

            var content    = NewMessage.Trim();
            var receiverId = SelectedConv.EmployeId;

            // Affichage optimiste immédiat
            var optimisticMsg = new ChatMessageDto
            {
                Id         = -(CurrentMessages.Count + 1), // ID temporaire négatif
                SenderId   = CurrentUserId,
                ReceiverId = receiverId,
                Content    = content,
                SentAt     = DateTime.Now,
                IsRead     = false
            };
            CurrentMessages.Add(optimisticMsg);
            UpdateConversationWithMessage(optimisticMsg);
            NewMessage = string.Empty;
            StateHasChanged();
            await ScrollToBottomAsync();

            // Envoi via SignalR — senderId passé explicitement (pas de claim JWT côté hub)
            try
            {
                await _hub.SendAsync("SendMessage", CurrentUserId, receiverId, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur envoi message: {ex.Message}");
            }

            await SendTypingIndicatorAsync(false);
        }

        private async Task OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !e.ShiftKey) { await SendMessage(); return; }

            if (!_isTyping) { _isTyping = true; await SendTypingIndicatorAsync(true); }

            _typingTimer?.Stop();
            _typingTimer = new System.Timers.Timer(1500);
            _typingTimer.Elapsed += async (_, _) =>
            {
                _typingTimer?.Stop();
                _isTyping = false;
                await InvokeAsync(() => SendTypingIndicatorAsync(false));
            };
            _typingTimer.AutoReset = false;
            _typingTimer.Start();
        }

        private async Task SendTypingIndicatorAsync(bool isTyping)
        {
            if (_hub?.State == HubConnectionState.Connected && SelectedConv != null)
                try { await _hub.SendAsync("Typing", CurrentUserId, SelectedConv.EmployeId, isTyping); } catch { }
        }

        private async Task MarkMessagesAsReadAsync(int senderId)
        {
            foreach (var m in CurrentMessages.Where(m => m.SenderId == senderId && !m.IsRead))
                m.IsRead = true;
            if (_hub?.State == HubConnectionState.Connected)
                try { await _hub.SendAsync("MarkRead", CurrentUserId, senderId); } catch { }
        }

        private void UpdateConversationWithMessage(ChatMessageDto msg)
        {
            var otherId = msg.SenderId == CurrentUserId ? msg.ReceiverId : msg.SenderId;
            var conv = Conversations.FirstOrDefault(c => c.EmployeId == otherId);
            if (conv == null) return;
            conv.LastMessage     = msg.Content;
            conv.LastMessageTime = msg.SentAt;
            if (msg.SenderId != CurrentUserId && SelectedConv?.EmployeId != otherId)
                conv.UnreadCount++;
        }

        private void BackToList() { _conversationOpen = false; SelectedConv = null; }

        private async Task ScrollToBottomAsync()
        {
            await Task.Delay(50);
            try { await JS.InvokeVoidAsync("scrollToBottom", "messagesZone"); } catch { }
        }

        private static string GroupDateLabel(DateTime dt)
        {
            var today = DateTime.Today;
            if (dt.Date == today) return "Aujourd'hui";
            if (dt.Date == today.AddDays(-1)) return "Hier";
            if ((today - dt.Date).TotalDays < 7) return dt.ToString("dddd", new System.Globalization.CultureInfo("fr-FR"));
            return dt.ToString("dd/MM/yyyy");
        }

        private static string FormatConvTime(DateTime? dt)
        {
            if (!dt.HasValue) return string.Empty;
            var today = DateTime.Today;
            if (dt.Value.Date == today) return dt.Value.ToString("HH:mm");
            if (dt.Value.Date == today.AddDays(-1)) return "Hier";
            return dt.Value.ToString("dd/MM");
        }

        private string GetInitials()
        {
            var parts = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
            return "IT";
        }

        public async ValueTask DisposeAsync()
        {
            VoiceSvc.OnCommand -= HandleVoiceCommand;
            _typingTimer?.Dispose();
            if (_hub != null)
            {
                if (_hub.State == HubConnectionState.Connected)
                    try { await _hub.SendAsync("UserDisconnected", CurrentUserId); } catch { }
                await _hub.DisposeAsync();
            }
        }
    }
}