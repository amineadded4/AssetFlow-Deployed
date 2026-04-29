// src/Backend/AssetFlow.WebAPI/Controllers/ConversationController.cs
using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/conversations")]
    [Authorize]
    public class ConversationController : ControllerBase
    {
        private readonly IConversationHistoryService _svc;

        public ConversationController(IConversationHistoryService svc)
        {
            _svc = svc;
        }

        // ── Récupérer l'userId depuis le JWT ─────────────────────────────────
        private int GetUserId()
        {
            var raw = User.FindFirstValue("userId")
                   ?? User.FindFirstValue("db_user_id")
                   ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(raw, out var id) ? id : 0;
        }

        // ── POST /api/conversations — créer une conversation ─────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateConversationRequest req)
        {
            var userId = req.UserId > 0 ? req.UserId : GetUserId();
            if (userId == 0) return BadRequest("userId manquant.");

            var title  = string.IsNullOrWhiteSpace(req.Title) ? "Nouvelle conversation" : req.Title;
            var convId = await _svc.CreateConversationAsync(userId, title);

            return Ok(new CreateConversationResponse
            {
                ConversationId = convId,
                Title          = title,
                CreatedAt      = DateTime.UtcNow
            });
        }

        // ── GET /api/conversations?userId=X — liste ──────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int userId)
        {
            if (userId == 0) userId = GetUserId();
            if (userId == 0) return BadRequest("userId manquant.");

            var convs = await _svc.GetConversationsAsync(userId);
            return Ok(new ConversationListResponse
            {
                Conversations = convs.Select(c => new ConversationSummaryDto
                {
                    Id           = c.Id,
                    Title        = c.Title,
                    CreatedAt    = c.CreatedAt,
                    UpdatedAt    = c.UpdatedAt,
                    MessageCount = c.MessageCount,
                    LastMessage  = c.LastMessage
                }).ToList()
            });
        }

        // ── GET /api/conversations/{id}/messages — messages ──────────────────
        [HttpGet("{conversationId}/messages")]
        public async Task<IActionResult> GetMessages(string conversationId)
        {
            var messages = await _svc.GetMessagesAsync(conversationId);
            return Ok(new ConversationMessagesResponse
            {
                ConversationId = conversationId,
                Messages = messages.Select(m => new ConversationMessageDto
                {
                    Id              = m.Id,
                    Role            = m.Role,
                    Content         = m.Content,
                    AgentUsed       = m.AgentUsed,
                    Timestamp       = m.Timestamp,
                    ActionJson      = m.ActionJson,
                    ActionProcessed = m.ActionProcessed,
                    OffersJson      = m.OffersJson   // ← AJOUT : cartes d'offres restaurées
                }).ToList()
            });
        }

        // ── POST /api/conversations/{id}/messages — ajouter un message ────────
        [HttpPost("{conversationId}/messages")]
        public async Task<IActionResult> AddMessage(string conversationId, [FromBody] AddMessageRequest req)
        {
            await _svc.AddMessageAsync(conversationId, new ConversationMessage
            {
                Role            = req.Role,
                Content         = req.Content,
                AgentUsed       = req.AgentUsed,
                ActionJson      = req.ActionJson,
                ActionProcessed = req.ActionProcessed,
                Timestamp       = DateTime.UtcNow,
                OffersJson      = req.OffersJson
            });
            return Ok();
        }

        // ── PUT /api/conversations/{id}/title — renommer ─────────────────────
        [HttpPut("{conversationId}/title")]
        public async Task<IActionResult> UpdateTitle(string conversationId, [FromBody] UpdateTitleRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest("Titre vide.");

            await _svc.UpdateTitleAsync(conversationId, req.Title);
            return Ok();
        }

        // ── DELETE /api/conversations/{id} — supprimer ───────────────────────
        [HttpDelete("{conversationId}")]
        public async Task<IActionResult> Delete(string conversationId, [FromQuery] int userId)
        {
            if (userId == 0) userId = GetUserId();
            await _svc.DeleteConversationAsync(conversationId, userId);
            return Ok();
        }

        // ── DELETE /api/conversations/all?userId=X — tout supprimer ─────────
        [HttpDelete("all")]
        public async Task<IActionResult> DeleteAll([FromQuery] int userId)
        {
            if (userId == 0) userId = GetUserId();
            if (userId == 0) return BadRequest("userId manquant.");
            await _svc.DeleteAllConversationsAsync(userId);
            return Ok();
        }
    }
}
