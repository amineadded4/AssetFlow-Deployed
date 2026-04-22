using System.Net.Http.Json;
using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using AssetFlow.BlazorUI.DTOs;

namespace AssetFlow.BlazorUI.Pages.Employe
{
    public partial class MessagerieEmploye : IAsyncDisposable
    {
        [Inject] private MessagerieService    MsgSvc       { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;
        [Inject] private HttpClient           Http         { get; set; } = default!;
        [Inject] private IJSRuntime           JS           { get; set; } = default!;

        private int  CurrentUserId     = 0;
        private bool _hubConnected     = false;
        private bool _conversationOpen = false;
        private bool LoadingITUsers    = true;
        private bool LoadingMessages   = false;

        private string SearchIT   { get; set; } = string.Empty;
        private string NewMessage { get; set; } = string.Empty;

        private List<ITUserConvDto>  ITUsers    { get; set; } = new();
        private ITUserConvDto?       SelectedIT { get; set; }
        private List<ChatMessageDto> Messages   { get; set; } = new();

        private HubConnection?       _hub;
        private System.Timers.Timer? _typingTimer;
        private bool                 _isTyping  = false;
        private bool                 _menuOpen  = false;

        // ── Voice recording ──────────────────────────────────────────────────
        private bool   _isRecording      = false;
        private bool   _hasVoiceBlob     = false;
        private double _recordingSeconds = 0;
        private System.Timers.Timer? _recordTimer;
        private const int MaxVoiceSeconds = 60;
        // ─────────────────────────────────────────────────────────────────────

        private List<ITUserConvDto> ITUsersFiltres =>
            string.IsNullOrWhiteSpace(SearchIT)
                ? ITUsers.OrderByDescending(u => u.LastMessageTime ?? DateTime.MinValue).ToList()
                : ITUsers.Where(u => u.FullName.Contains(SearchIT, StringComparison.OrdinalIgnoreCase))
                         .OrderByDescending(u => u.LastMessageTime ?? DateTime.MinValue).ToList();

        private Dictionary<string, List<ChatMessageDto>> GroupedMessages =>
            Messages.GroupBy(m => GroupDateLabel(m.SentAt))
                    .ToDictionary(g => g.Key, g => g.ToList());

        protected override async Task OnInitializedAsync()
        {
            CurrentUserId = await LocalStorage.GetItemAsync<int>("user_id");
            await LoadITUsersAsync();
            await ConnectHubAsync();
        }

        // ── Connexion SignalR ─────────────────────────────────────────────────
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

                // ── Réception d'un message (texte ou vocal) ──────────────────
                _hub.On<ChatMessageDto>("ReceiveMessage", async msg =>
                {
                    await InvokeAsync(async () =>
                    {
                        var otherId = msg.SenderId == CurrentUserId ? msg.ReceiverId : msg.SenderId;
                        if (SelectedIT?.Id == otherId)
                        {
                            if (msg.SenderId != CurrentUserId)
                            {
                                Messages.Add(msg);
                            }
                            else
                            {
                                // Mise à jour du message optimiste (texte ou vocal)
                                var opt = Messages.LastOrDefault(m => m.SenderId == CurrentUserId && m.Id < 0);
                                if (opt != null)
                                {
                                    opt.Id                   = msg.Id;
                                    opt.AudioData            = msg.AudioData;
                                    opt.AudioDurationSeconds = msg.AudioDurationSeconds;
                                }
                            }
                            if (msg.SenderId != CurrentUserId)
                                await MarkMessagesAsReadAsync(msg.SenderId);
                        }
                        UpdateITWithMessage(msg);
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
                        var it = ITUsers.FirstOrDefault(u => u.Id == userId);
                        if (it != null) it.IsOnline = isOnline;
                        if (SelectedIT?.Id == userId) SelectedIT.IsOnline = isOnline;
                        StateHasChanged();
                    });
                });

                _hub.On<int, bool>("UserTyping", async (userId, isTyping) =>
                {
                    await InvokeAsync(() =>
                    {
                        if (SelectedIT?.Id == userId) SelectedIT.IsTyping = isTyping;
                        StateHasChanged();
                    });
                });

                _hub.On<List<int>>("OnlineUsers", async (onlineUserIds) =>
                {
                    await InvokeAsync(() =>
                    {
                        foreach (var it in ITUsers)
                            it.IsOnline = onlineUserIds.Contains(it.Id);
                        if (SelectedIT != null)
                            SelectedIT.IsOnline = onlineUserIds.Contains(SelectedIT.Id);
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

        // ── Chargement des agents IT ──────────────────────────────────────────
        private async Task LoadITUsersAsync()
        {
            LoadingITUsers = true;
            StateHasChanged();

            try
            {
                var itUsers = await Http.GetFromJsonAsync<List<ITUserSimpleDto>>("api/users/it") ?? new();
                ITUsers = itUsers.Select(u => new ITUserConvDto
                {
                    Id       = u.Id,
                    FullName = u.FullName,
                    Initials = u.Initials,
                }).ToList();

                var summaries = await MsgSvc.GetConversationsAsync(CurrentUserId);
                foreach (var s in summaries)
                {
                    var it = ITUsers.FirstOrDefault(u => u.Id == s.OtherUserId);
                    if (it != null)
                    {
                        it.LastMessage     = s.LastMessage;
                        it.LastMessageTime = s.LastMessageTime;
                        it.UnreadCount     = s.UnreadCount;
                    }
                }
            }
            catch { }

            LoadingITUsers = false;
            StateHasChanged();
        }

        // ── Sélection d'un agent IT ───────────────────────────────────────────
        private async Task SelectIT(ITUserConvDto it)
        {
            SelectedIT        = it;
            _conversationOpen = true;
            it.UnreadCount    = 0;
            LoadingMessages   = true;
            StateHasChanged();

            Messages = await MsgSvc.GetHistoryAsync(CurrentUserId, it.Id);
            LoadingMessages = false;
            await MarkMessagesAsReadAsync(it.Id);
            StateHasChanged();
            await ScrollToBottomAsync();
        }

        // ── Envoi message texte ───────────────────────────────────────────────
        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(NewMessage) || SelectedIT == null) return;
            if (_hub?.State != HubConnectionState.Connected) return;

            var content    = NewMessage.Trim();
            var receiverId = SelectedIT.Id;

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
            UpdateITWithMessage(optimistic);
            NewMessage = string.Empty;
            StateHasChanged();
            await ScrollToBottomAsync();

            try { await _hub.SendAsync("SendMessage", CurrentUserId, receiverId, content); }
            catch (Exception ex) { Console.WriteLine($"Erreur envoi: {ex.Message}"); }

            await SendTypingIndicatorAsync(false);
        }

        // ── Voice recording ───────────────────────────────────────────────────

        private async Task StartRecording()
        {
            _isRecording      = true;
            _hasVoiceBlob     = false;
            _recordingSeconds = 0;
            StateHasChanged();

            _recordTimer = new System.Timers.Timer(1000);
            _recordTimer.Elapsed += async (_, _) =>
            {
                _recordingSeconds++;
                if (_recordingSeconds >= MaxVoiceSeconds)
                    await InvokeAsync(StopRecording);
                else
                    await InvokeAsync(StateHasChanged);
            };
            _recordTimer.AutoReset = true;
            _recordTimer.Start();

            try { await JS.InvokeVoidAsync("voiceRecorder.start"); }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur microphone: {ex.Message}");
                _isRecording = false;
                _recordTimer?.Dispose();
                _recordTimer = null;
                StateHasChanged();
            }
        }

        private async Task StopRecording()
        {
            _recordTimer?.Stop();
            _recordTimer?.Dispose();
            _recordTimer  = null;
            _isRecording  = false;

            try
            {
                var base64 = await JS.InvokeAsync<string>("voiceRecorder.stop");
                if (!string.IsNullOrEmpty(base64))
                    _hasVoiceBlob = true;
            }
            catch (Exception ex) { Console.WriteLine($"Erreur stop recording: {ex.Message}"); }

            StateHasChanged();
        }

        private async Task CancelVoice()
        {
            _recordTimer?.Stop();
            _recordTimer?.Dispose();
            _recordTimer      = null;
            _isRecording      = false;
            _hasVoiceBlob     = false;
            _recordingSeconds = 0;
            try { await JS.InvokeVoidAsync("voiceRecorder.cancel"); } catch { }
            StateHasChanged();
        }

        private async Task SendVoiceMessage()
        {
            if (SelectedIT == null || _hub?.State != HubConnectionState.Connected) return;

            string base64;
            try { base64 = await JS.InvokeAsync<string>("voiceRecorder.getBlob"); }
            catch { return; }

            if (string.IsNullOrEmpty(base64)) return;

            var receiverId = SelectedIT.Id;
            var duration   = (int)Math.Min(_recordingSeconds, MaxVoiceSeconds);

            // Message optimiste affiché immédiatement
            var optimistic = new ChatMessageDto
            {
                Id                   = -(Messages.Count + 1),
                SenderId             = CurrentUserId,
                ReceiverId           = receiverId,
                Content              = string.Empty,
                AudioData            = base64,
                AudioDurationSeconds = duration,
                SentAt               = DateTime.Now,
                IsRead               = false
            };
            Messages.Add(optimistic);
            UpdateITWithMessage(optimistic);

            _hasVoiceBlob     = false;
            _recordingSeconds = 0;
            StateHasChanged();
            await ScrollToBottomAsync();

            // Envoi via Hub
            try
            {
                await _hub.SendAsync("SendVoiceMessage", CurrentUserId, receiverId, base64, duration);
            }
            catch (Exception ex) { Console.WriteLine($"Erreur envoi vocal: {ex.Message}"); }
        }

        private string FormatRecordTime(double secs)
        {
            var m = (int)secs / 60;
            var s = (int)secs % 60;
            return $"{m}:{s:D2}";
        }

        // ── Génération déterministe des hauteurs de la waveform ───────────────
        // Donne un profil "naturel" stable par message, identique à chaque rendu.
        private int GetBarHeight(int msgId, int index)
        {
            unchecked
            {
                var seed = (msgId * 9176 + index * 31337) ^ 0x5A5A5A5A;
                var v = (Math.Abs(seed) % 80) + 20; // 20..100
                // Légère atténuation aux extrémités pour un look pro
                if (index < 2 || index > 29) v = Math.Min(v, 35);
                return v;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

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
            if (_hub?.State == HubConnectionState.Connected && SelectedIT != null)
                try { await _hub.SendAsync("Typing", CurrentUserId, SelectedIT.Id, isTyping); } catch { }
        }

        private async Task MarkMessagesAsReadAsync(int senderId)
        {
            foreach (var m in Messages.Where(m => m.SenderId == senderId && !m.IsRead))
                m.IsRead = true;
            if (_hub?.State == HubConnectionState.Connected)
                try { await _hub.SendAsync("MarkRead", CurrentUserId, senderId); } catch { }
        }

        private void UpdateITWithMessage(ChatMessageDto msg)
        {
            var otherId = msg.SenderId == CurrentUserId ? msg.ReceiverId : msg.SenderId;
            var it = ITUsers.FirstOrDefault(u => u.Id == otherId);
            if (it == null) return;
            // Prévisualisation : vocal → emoji, texte → contenu
            it.LastMessage     = msg.IsVoice ? "🎤 Message vocal" : msg.Content;
            it.LastMessageTime = msg.SentAt;
            if (msg.SenderId != CurrentUserId && SelectedIT?.Id != otherId)
                it.UnreadCount++;
        }

        private void BackToList() { _conversationOpen = false; SelectedIT = null; }

        private async Task ScrollToBottomAsync()
        {
            await Task.Delay(50);
            try { await JS.InvokeVoidAsync("scrollToBottom", "msgZoneEmploye"); } catch { }
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
            if (dt.Value.Date == today)             return dt.Value.ToString("HH:mm");
            if (dt.Value.Date == today.AddDays(-1)) return "Hier";
            return dt.Value.ToString("dd/MM");
        }

        public async ValueTask DisposeAsync()
        {
            _typingTimer?.Dispose();
            _recordTimer?.Dispose();
            if (_hub != null)
            {
                if (_hub.State == HubConnectionState.Connected)
                    try { await _hub.SendAsync("UserDisconnected", CurrentUserId); } catch { }
                await _hub.DisposeAsync();
            }
        }
    }
}
