// ============================================================
// AssetFlow.WebAPI / Controllers / ArticlesController.cs
// ============================================================

using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/articles")]
    [Authorize(Policy = "AchatOrAdmin")]
    public class ArticlesController : ControllerBase
    {
        private readonly IArticleService _articleService;
        public ArticlesController(IArticleService articleService) => _articleService = articleService;

        // PATCH api/articles/{id}/numero-serie
        [HttpPatch("{id:int}/numero-serie")]
        public async Task<IActionResult> UpdateNumeroSerie(int id, [FromBody] UpdateNumeroSerieDto dto)
        {
            var (success, message, numeroSerie) = await _articleService.UpdateNumeroSerieAsync(id, dto.NumeroSerie);

            if (!success)
            {
                // 404 si article introuvable, 400 sinon
                return message.Contains("introuvable")
                    ? NotFound(new { Message = message })
                    : BadRequest(new { Message = message });
            }

            return Ok(new { Message = message, NumeroSerie = numeroSerie });
        }
    }

    public class UpdateNumeroSerieDto
    {
        public string? NumeroSerie { get; set; }
    }
}