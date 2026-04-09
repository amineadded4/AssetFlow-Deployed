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
        [HttpGet("{userId:int}/{otherUserId:int}")]
        public async Task<IActionResult> GetHistory(int userId, int otherUserId)
        {
            var messages = await _db.ChatMessages
                .Where(m => (m.SenderId == userId      && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.ReceiverId,
                    m.Content,
                    m.SentAt,
                    m.IsRead,
                    // ── champs vocaux ──────────────────────────────────
                    m.AudioData,
                    m.AudioDurationSeconds
                    // ──────────────────────────────────────────────────
                })
                .ToListAsync();

            return Ok(messages);
        }

        // GET api/messages/conversations/{userId}
        [HttpGet("conversations/{userId:int}")]
        public async Task<IActionResult> GetConversations(int userId)
        {
            var partnerIds = await _db.ChatMessages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var result = new List<object>();

            foreach (var partnerId in partnerIds)
            {
                var lastMsg = await _db.ChatMessages
                    .Where(m => (m.SenderId == userId    && m.ReceiverId == partnerId) ||
                                (m.SenderId == partnerId && m.ReceiverId == userId))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                var unread = await _db.ChatMessages
                    .CountAsync(m => m.SenderId == partnerId && m.ReceiverId == userId && !m.IsRead);

                // Pour les vocaux, afficher un label au lieu du contenu binaire
                var lastMsgContent = lastMsg == null
                    ? null
                    : lastMsg.AudioData != null
                        ? "🎤 Message vocal"
                        : lastMsg.Content;

                result.Add(new
                {
                    OtherUserId     = partnerId,
                    LastMessage     = lastMsgContent,
                    LastMessageTime = lastMsg?.SentAt,
                    UnreadCount     = unread
                });
            }

            return Ok(result);
        }
    }
}
