using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/chat-offre")]
    [Authorize(Policy = "ITOrAdmin")]
    public class ChatOffreController : ControllerBase
    {
        private readonly IChatOffreService _chatOffreService;

        public ChatOffreController(IChatOffreService chatOffreService)
        {
            _chatOffreService = chatOffreService;
        }

        // POST api/chat-offre/send
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] ChatOffreRequestDto dto)
        {
            var (reply, recommendedOffre) = await _chatOffreService.SendAsync(dto);
            return Ok(new { reply, recommendedOffre });
        }

        // GET api/chat-offre/history/{userId}/{idDemande}
        [HttpGet("history/{userId}/{idDemande:int}")]
        public async Task<IActionResult> GetHistory(string userId, int idDemande)
        {
            var history = await _chatOffreService.GetHistoryAsync(userId, idDemande);
            return Ok(history);
        }

        // DELETE api/chat-offre/history/{userId}/{idDemande}
        [HttpDelete("history/{userId}/{idDemande:int}")]
        public async Task<IActionResult> DeleteHistory(string userId, int idDemande)
        {
            await _chatOffreService.DeleteHistoryAsync(userId, idDemande);
            return Ok();
        }

        // GET api/chat-offre/recommendation/{userId}/{idDemande}
        [HttpGet("recommendation/{userId}/{idDemande:int}")]
        public async Task<IActionResult> GetRecommendation(string userId, int idDemande)
        {
            var rec = await _chatOffreService.GetRecommendationAsync(userId, idDemande);
            return Ok(new { recommendedOffre = rec });
        }
    }
}