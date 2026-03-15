// ============================================================
// AssetFlow.WebAPI / Controllers / DemandeAchatITController.cs
// Route : api/it/demandesachat
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/it/demandesachat")]
    [Authorize(Policy = "ITOnly")]
    public class DemandeAchatITController : ControllerBase
    {
        private readonly IDemandeAchatITService _service;

        public DemandeAchatITController(IDemandeAchatITService service)
        {
            _service = service;
        }

        // ── GET api/it/demandesachat ──────────────────────────────
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DemandeAchatITDto>>> GetAll()
        {
            var demandes = await _service.GetAllAsync();
            return Ok(demandes);
        }

        // ── GET api/it/demandesachat/{id} ─────────────────────────
        [HttpGet("{id:int}")]
        public async Task<ActionResult<DemandeAchatITDto>> GetById(int id)
        {
            var demande = await _service.GetByIdAsync(id);
            if (demande == null) return NotFound();
            return Ok(demande);
        }

        // ── POST api/it/demandesachat ─────────────────────────────
        // Reçoit le formulaire de création depuis la page Blazor IT.
        // Retourne 201 Created avec le DTO de la demande créée.
        [HttpPost]
        public async Task<ActionResult<DemandeAchatITDto>> Create(
            [FromBody] CreateDemandeAchatDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NomProduit))
                return BadRequest("Le nom du produit est obligatoire.");

            /*if (dto.Quantite < 1)
                return BadRequest("La quantité doit être au moins 1.");*/

            var created = await _service.CreateAsync(dto);

            // Retourne 201 Created + l'URL de la nouvelle ressource
            return CreatedAtAction(
                nameof(GetById),
                new { id = created.IdDemande },
                created);
        }
    }
}
