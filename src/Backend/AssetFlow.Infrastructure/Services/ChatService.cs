using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Application.DTOs;

namespace AssetFlow.Infrastructure.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _db;

        public ChatService(AppDbContext db) => _db = db;

        public async Task<ChatMessagePayload> SendMessageAsync(int senderId, int receiverId, string content)
        {
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

            return new ChatMessagePayload
            {
                Id         = msg.Id,
                SenderId   = msg.SenderId,
                ReceiverId = msg.ReceiverId,
                Content    = msg.Content,
                SentAt     = msg.SentAt,
                IsRead     = false
            };
        }

        public async Task MarkReadAsync(int readerId, int senderId)
        {
            var unread = await _db.ChatMessages
                .Where(m => m.SenderId == senderId && m.ReceiverId == readerId && !m.IsRead)
                .ToListAsync();

            foreach (var m in unread) m.IsRead = true;
            await _db.SaveChangesAsync();
        }

        public Task<List<int>> GetOnlineUsersAsync()
        {
            // La liste en mémoire est gérée par le Hub — on délègue via le ConnectionTracker
            throw new NotImplementedException("Utiliser IConnectionTracker pour accéder aux users en ligne.");
        }
    }
}