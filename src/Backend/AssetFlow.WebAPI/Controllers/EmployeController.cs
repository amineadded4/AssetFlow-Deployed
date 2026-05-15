using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeController : ControllerBase
    {
        private readonly IEmployeService _employeService;

        public EmployeController(IEmployeService employeService)
        {
            _employeService = employeService;
        }

        // GET api/employe/equipements/{utilisateurId}
        [HttpGet("equipements/{utilisateurId}")]
        public async Task<IActionResult> GetEquipementsAffectes(int utilisateurId)
        {
            if (utilisateurId <= 0) return BadRequest("ID utilisateur invalide.");
            var equipements = await _employeService.GetEquipementsAffectesAsync(utilisateurId);
            return Ok(equipements);
        }
        // GET api/employe/equipements/detail/{affectationId}
        [AllowAnonymous]
        [HttpGet("equipements/detail/{affectationId}")]
        public async Task<IActionResult> GetEquipementDetail(int affectationId, [FromQuery] int articleId = 0)
        {
            var equipement = await _employeService.GetEquipementDetailAsync(affectationId, articleId); 
            return Ok(equipement);
        }
        // GET api/employe/{utilisateurId}/materiels-groupes
        // Retourne les matériels groupés avec leurs articles individuels.
        // C'est l'endpoint utilisé par la nouvelle grille MesEquipements.
        [HttpGet("{utilisateurId}/materiels-groupes")]
        public async Task<IActionResult> GetMaterielsGroupes(int utilisateurId)
        {
            if (utilisateurId <= 0) return BadRequest("ID utilisateur invalide.");
            var groupes = await _employeService.GetMaterielsGroupesAsync(utilisateurId);
            return Ok(groupes);
        }
    }
}