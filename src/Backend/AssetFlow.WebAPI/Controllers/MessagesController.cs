using AssetFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
 
namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/messages")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public MessagesController(AppDbContext db) => _db = db;
 
        // GET api/messages/{userId}/{otherUserId}
        // Historique complet entre deux utilisateurs
        [HttpGet("{userId:int}/{otherUserId:int}")]
        public async Task<IActionResult> GetHistory(int userId, int otherUserId)
        {
            var messages = await _db.ChatMessages
                .Where(m => (m.SenderId == userId   && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.ReceiverId,
                    m.Content,
                    m.SentAt,
                    m.IsRead
                })
                .ToListAsync();
 
            return Ok(messages);
        }
 
        // GET api/messages/conversations/{userId}
        // Résumé de toutes les conversations (dernier msg + non-lus)
        [HttpGet("conversations/{userId:int}")]
        public async Task<IActionResult> GetConversations(int userId)
        {
            // Toutes les personnes avec qui userId a échangé
            var partnerIds = await _db.ChatMessages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();
 
            var result = new List<object>();
 
            foreach (var partnerId in partnerIds)
            {
                var lastMsg = await _db.ChatMessages
                    .Where(m => (m.SenderId == userId   && m.ReceiverId == partnerId) ||
                                (m.SenderId == partnerId && m.ReceiverId == userId))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();
 
                var unread = await _db.ChatMessages
                    .CountAsync(m => m.SenderId == partnerId && m.ReceiverId == userId && !m.IsRead);
 
                result.Add(new
                {
                    OtherUserId     = partnerId,
                    LastMessage     = lastMsg?.Content,
                    LastMessageTime = lastMsg?.SentAt,
                    UnreadCount     = unread
                });
            }
 
            return Ok(result);
        }
    }
}