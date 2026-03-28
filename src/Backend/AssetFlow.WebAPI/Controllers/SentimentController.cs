// ============================================================
// AssetFlow.WebAPI / Controllers / SentimentController.cs
// ============================================================

using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SentimentController : ControllerBase
    {
        private readonly ISentimentService _service;

        public SentimentController(ISentimentService service)
        {
            _service = service;
        }

        /// <summary>
        /// GET api/sentiment/materiel/{materielId}
        /// Analyse de sentiment pour un matériel donné
        /// </summary>
        [HttpGet("materiel/{materielId}")]
        public async Task<IActionResult> AnalyserMateriel(int materielId)
        {
            if (materielId <= 0) return BadRequest("ID matériel invalide.");
            try
            {
                var result = await _service.AnalyserSentimentMaterielAsync(materielId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// GET api/sentiment/tous
        /// Analyse de sentiment pour tous les matériels commentés
        /// </summary>
        [HttpGet("tous")]
        public async Task<IActionResult> AnalyserTous()
        {
            var results = await _service.AnalyserTousMaterielAsync();
            return Ok(results);
        }
    }
}
