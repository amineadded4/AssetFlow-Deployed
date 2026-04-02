using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/affectation")]
    [Authorize(Policy = "ITOrAdmin")]
    public class AffectationController : ControllerBase
    {
        private readonly IAffectationService _svc;
        public AffectationController(IAffectationService svc) => _svc = svc;

        // GET api/affectation/utilisateurs?search=...
        [HttpGet("utilisateurs")]
        public async Task<IActionResult> GetUtilisateurs([FromQuery] string? search = null)
            => Ok(await _svc.GetUtilisateursDisponiblesAsync(search));

        // GET api/affectation/materiels?search=...
        [HttpGet("materiels")]
        public async Task<IActionResult> GetMateriels([FromQuery] string? search = null)
            => Ok(await _svc.GetMaterielsDisponiblesAsync(search));

        // GET api/affectation/projets?search=...  
        [HttpGet("projets")]
        public async Task<IActionResult> GetProjets([FromQuery] string? search = null)
            => Ok(await _svc.GetProjetsDisponiblesAsync(search));

        // POST api/affectation
        [HttpPost]
        public async Task<IActionResult> CreerAffectation([FromBody] CreerAffectationDto dto)
        {
            if (dto.MaterielId <= 0 || dto.UtilisateurId <= 0)
                return BadRequest("MaterielId et UtilisateurId sont requis.");

            var result = await _svc.CreerAffectationAsync(dto);
            return result.Succes ? Ok(result) : BadRequest(result);
        }
    }
}