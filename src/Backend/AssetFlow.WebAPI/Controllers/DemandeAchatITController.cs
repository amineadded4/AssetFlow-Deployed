// ============================================================
// AssetFlow.WebAPI / Controllers / DemandeAchatITController.cs
// MODIF : GET accepte userId en query param (nullable) pour filtrer
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/it/demandesachat")]
    [Authorize(Policy = "ITOrAdmin")]
    public class DemandeAchatITController : ControllerBase
    {
        private readonly IDemandeAchatITService _service;

        public DemandeAchatITController(IDemandeAchatITService service)
        {
            _service = service;
        }

        // GET api/it/demandesachat?userId=5
        // userId est nullable : si absent → toutes les demandes (utile pour Admin)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DemandeAchatITDto>>> GetAll(
            [FromQuery] int? userId = null)
        {
            var demandes = await _service.GetAllAsync(userId);
            return Ok(demandes);
        }

        // GET api/it/demandesachat/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<DemandeAchatITDto>> GetById(int id)
        {
            var demande = await _service.GetByIdAsync(id);
            if (demande == null) return NotFound();
            return Ok(demande);
        }

        // POST api/it/demandesachat
        [HttpPost]
        public async Task<ActionResult<DemandeAchatITDto>> Create(
            [FromBody] CreateDemandeAchatDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NomProduit))
                return BadRequest("Le nom du produit est obligatoire.");

            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.IdDemande }, created);
        }

        // PUT api/it/demandesachat/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<DemandeAchatITDto>> Update(
            int id, [FromBody] UpdateDemandeAchatDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NomProduit))
                return BadRequest("Le nom du produit est obligatoire.");

            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // DELETE api/it/demandesachat/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}