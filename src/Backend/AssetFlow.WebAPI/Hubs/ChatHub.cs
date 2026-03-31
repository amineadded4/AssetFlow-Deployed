// ============================================================
// AssetFlow.WebAPI / Hubs / ChatHub.cs
// ============================================================

using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AssetFlow.WebAPI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService        _chatService;
        private readonly IConnectionTracker  _tracker;

        public ChatHub(IChatService chatService, IConnectionTracker tracker)
        {
            _chatService = chatService;
            _tracker     = tracker;
        }

        // ── Connexion ────────────────────────────────────────────────────────

        public async Task UserConnected(int userId)
        {
            _tracker.Add(userId, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            await Clients.Others.SendAsync("UserOnlineStatus", userId, true);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = _tracker.GetUserId(Context.ConnectionId);
            if (userId.HasValue)
            {
                bool isFullyOffline = _tracker.Remove(userId.Value, Context.ConnectionId);
                if (isFullyOffline)
                    await Clients.Others.SendAsync("UserOnlineStatus", userId.Value, false);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ── Envoi de message ─────────────────────────────────────────────────

        public async Task SendMessage(int senderId, int receiverId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            var dto = await _chatService.SendMessageAsync(senderId, receiverId, content);

            await Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", dto);
            await Clients.Caller.SendAsync("ReceiveMessage", dto);
        }

        // ── Indicateur de frappe ─────────────────────────────────────────────

        public async Task Typing(int senderId, int receiverId, bool isTyping)
        {
            await Clients.Group($"user_{receiverId}").SendAsync("UserTyping", senderId, isTyping);
        }

        // ── Marquer comme lu ─────────────────────────────────────────────────

        public async Task MarkRead(int readerId, int senderId)
        {
            await _chatService.MarkReadAsync(readerId, senderId);
            await Clients.Group($"user_{senderId}").SendAsync("MessagesRead", readerId, senderId);
        }

        // ── Users en ligne ───────────────────────────────────────────────────

        public async Task GetOnlineUsers()
        {
            var onlineIds = _tracker.GetOnlineUserIds();
            await Clients.Caller.SendAsync("OnlineUsers", onlineIds);
        }
    }

    public class ChatMessagePayload
    {
        public int      Id         { get; set; }
        public int      SenderId   { get; set; }
        public int      ReceiverId { get; set; }
        public string   Content    { get; set; } = string.Empty;
        public DateTime SentAt     { get; set; }
        public bool     IsRead     { get; set; }
    }
}