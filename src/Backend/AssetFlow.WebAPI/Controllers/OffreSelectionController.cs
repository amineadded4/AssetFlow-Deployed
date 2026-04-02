// AssetFlow.WebAPI/Controllers/OffreSelectionController.cs

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/offre-selection")]
    [Authorize(Policy = "ITOrAdmin")]
    public class OffreSelectionController : ControllerBase
    {
        private readonly IOffreSelectionService _selectionService;

        public OffreSelectionController(IOffreSelectionService selectionService)
        {
            _selectionService = selectionService;
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] OffreSelectionDto dto)
        {
            if (dto.OffreId == Guid.Empty) return BadRequest("offreId requis.");
            if (dto.IdDemande == 0)        return BadRequest("idDemande requis.");

            var (success, error) = await _selectionService.ConfirmSelectionAsync(dto);
            if (!success) return NotFound(error);

            return Ok(new { success = true });
        }
    }
}