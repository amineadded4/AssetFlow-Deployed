using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/materiel")]
    [Authorize(Policy = "AchatOrAdmin")]
    public class MaterielController : ControllerBase
    {
        private readonly IMaterielService _svc;
        public MaterielController(IMaterielService svc) => _svc = svc;

        // GET api/materiel
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _svc.GetAllAsync());

        // GET api/materiel/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var m = await _svc.GetByIdAsync(id);
            return m is null ? NotFound() : Ok(m);
        }

        // GET api/materiel/search?terme=...&categorie=...&etat=...
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? terme,
            [FromQuery] string? categorie,
            [FromQuery] string? etat)
        {
            var result = await _svc.SearchAsync(terme, categorie);
            return Ok(result);
        }

        // GET api/materiel/stats
        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
            => Ok(await _svc.GetStatsAsync());

        // POST api/materiel
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreerMaterielDto dto)
        {
            var result = await _svc.CreerAsync(dto);
            return result.Succes ? Ok(result) : BadRequest(result);
        }

        // PUT api/materiel/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ModifierMaterielDto dto)
        {
            dto.Id = id;
            var result = await _svc.ModifierAsync(dto);
            return result.Succes ? Ok(result) : BadRequest(result);
        }

        // DELETE api/materiel/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _svc.SupprimerAsync(id);
            return result.Succes ? Ok(result) : BadRequest(result);
        }

        // DELETE api/materiel/{id}/cascade
        // Supprime le matériel + toutes ses affectations + tous les incidents associés
        [HttpDelete("{id:int}/cascade")]
        public async Task<IActionResult> DeleteCascade(int id)
        {
            var result = await _svc.SupprimerAvecCascadeAsync(id);
            return result.Succes ? Ok(result) : BadRequest(result);
        }
    }
}