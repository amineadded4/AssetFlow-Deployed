using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/articles")]
    [Authorize(Policy = "ITOrAchatOrAdmin")]
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
                return message.Contains("introuvable") ? NotFound(new { Message = message }) : BadRequest(new { Message = message });
            return Ok(new { Message = message, NumeroSerie = numeroSerie });
        }

        // DELETE api/articles/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> SupprimerArticle(int id)
        {
            var (success, message) = await _articleService.SupprimerArticleAsync(id);
            if (!success)
                return message.Contains("introuvable") ? NotFound(new { Message = message }) : BadRequest(new { Message = message });
            return Ok(new { Message = message });
        }

        // PATCH api/articles/{id}/hors-service
        // Accessible aussi par IT → on élargit la policy ou on duplique le contrôleur IT
        [HttpPatch("{id:int}/hors-service")]
        [Authorize(Policy = "ITOrAdmin")]   // IT peut mettre hors service
        public async Task<IActionResult> MettreHorsService(int id)
        {
            var (success, message) = await _articleService.MettreHorsServiceAsync(id);
            if (!success)
                return message.Contains("introuvable") ? NotFound(new { Message = message }) : BadRequest(new { Message = message });
            return Ok(new { Message = message });
        }
        [HttpPatch("{id:int}/remettre-en-service")]
        [Authorize(Policy = "ITOrAdmin")]
        public async Task<IActionResult> RemettreEnService(int id)
        {
            var (success, message) = await _articleService.RemettreEnServiceAsync(id);
            if (!success)
                return message.Contains("introuvable") ? NotFound(new { Message = message }) : BadRequest(new { Message = message });
            return Ok(new { Message = message });
        }
    }

    public class UpdateNumeroSerieDto
    {
        public string? NumeroSerie { get; set; }
    }
}