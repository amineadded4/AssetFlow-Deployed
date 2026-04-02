using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/it/stock")]
    [Authorize(Policy = "ITOrAdmin")]
    public class StockITController : ControllerBase
    {
        private readonly IMaterielService _svc;
        public StockITController(IMaterielService svc) => _svc = svc;

        // GET api/it/stock
        // Liste complète des matériels (lecture seule)
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _svc.GetAllAsync());

        // GET api/it/stock/search?terme=...&categorie=...
        // Recherche filtrée
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? terme,
            [FromQuery] string? categorie)
            => Ok(await _svc.SearchAsync(terme, categorie));

        // GET api/it/stock/stats
        // KPI cards (totaux, alertes, ruptures)
        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
            => Ok(await _svc.GetStatsAsync());

        // GET api/it/stock/{id}
        // Détail d'un matériel
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var m = await _svc.GetByIdAsync(id);
            return m is null ? NotFound() : Ok(m);
        }

        // PATCH api/it/stock/{id}/seuil
        // Mise à jour du seuil minimum uniquement (pas de modif métier)
        [HttpPatch("{id:int}/seuil")]
        public async Task<IActionResult> UpdateSeuil(int id, [FromBody] UpdateSeuilDto dto)
        {
            if (dto.SeuilMin < 0 || dto.SeuilCritique < 0)
                return BadRequest("Les seuils doivent être positifs.");

            if (dto.SeuilCritique >= dto.SeuilMin)
                return BadRequest("Le seuil critique doit être strictement inférieur au seuil minimum.");

            var materiel = await _svc.GetByIdAsync(id);
            if (materiel is null)
                return NotFound("Matériel introuvable.");

            var updateDto = new ModifierMaterielDto
            {
                Id            = id,
                Reference     = materiel.Reference,
                Designation   = materiel.Designation,
                Description   = materiel.Description,
                Categorie     = materiel.Categorie,
                QuantiteStock = materiel.QuantiteStock,
                QuantiteMin   = dto.SeuilMin,        
                Unite         = materiel.Unite,
                Emplacement   = materiel.Emplacement,
                ImageUrl      = materiel.ImageUrl
            };

            var result = await _svc.ModifierAsync(updateDto);
            return result.Succes ? Ok(result) : BadRequest(result);
        }
    }
    public class UpdateSeuilDto
    {
        public int SeuilMin      { get; set; }
        public int SeuilCritique { get; set; }
    }
}