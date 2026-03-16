using AssetFlow.Infrastructure.Data;
using AssetFlow.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.WebAPI.Hubs
{
    // [Authorize] retiré — le userId est passé explicitement par le client
    // via UserConnected(userId) à la connexion
    public class ChatHub : Hub
    {
        private readonly AppDbContext _db;

        private static readonly Dictionary<int, HashSet<string>> _connections = new();
        private static readonly Dictionary<string, int> _connToUser = new(); // connectionId → userId
        private static readonly object _lock = new();

        public ChatHub(AppDbContext db) => _db = db;

        // ── Connexion ────────────────────────────────────────────────────────

        public async Task UserConnected(int userId)
        {
            lock (_lock)
            {
                if (!_connections.ContainsKey(userId))
                    _connections[userId] = new HashSet<string>();
                _connections[userId].Add(Context.ConnectionId);
                _connToUser[Context.ConnectionId] = userId;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            await Clients.Others.SendAsync("UserOnlineStatus", userId, true);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            int userId = 0;
            lock (_lock)
            {
                if (_connToUser.TryGetValue(Context.ConnectionId, out userId))
                {
                    _connToUser.Remove(Context.ConnectionId);
                    if (_connections.TryGetValue(userId, out var conns))
                    {
                        conns.Remove(Context.ConnectionId);
                        if (conns.Count == 0) _connections.Remove(userId);
                        else userId = 0; // encore connecté ailleurs → ne pas notifier offline
                    }
                }
            }
            if (userId > 0)
                await Clients.Others.SendAsync("UserOnlineStatus", userId, false);

            await base.OnDisconnectedAsync(exception);
        }

        // ── Envoi de message ─────────────────────────────────────────────────

        public async Task SendMessage(int senderId, int receiverId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            var msg = new ChatMessage
            {
                SenderId   = senderId,
                ReceiverId = receiverId,
                Content    = content.Trim(),
                SentAt     = DateTime.UtcNow,
                IsRead     = false
            };

            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            var dto = new ChatMessagePayload
            {
                Id         = msg.Id,
                SenderId   = msg.SenderId,
                ReceiverId = msg.ReceiverId,
                Content    = msg.Content,
                SentAt     = msg.SentAt,
                IsRead     = false
            };

            // Envoyer au destinataire
            await Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", dto);
            // Confirmer à l'expéditeur (pour mettre à jour l'Id réel du message)
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
            var unread = await _db.ChatMessages
                .Where(m => m.SenderId == senderId && m.ReceiverId == readerId && !m.IsRead)
                .ToListAsync();

            foreach (var m in unread) m.IsRead = true;
            await _db.SaveChangesAsync();

            await Clients.Group($"user_{senderId}").SendAsync("MessagesRead", readerId, senderId);
        }
        public async Task GetOnlineUsers()
        {
            List<int> onlineIds;
            lock (_lock)
            {
                onlineIds = _connections.Keys.ToList();
            }
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