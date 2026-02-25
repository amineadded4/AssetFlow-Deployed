// ============================================================
// AssetFlow.WebAPI / Controllers / EmployeController.cs
// MISE À JOUR : ajout endpoint GET materiels-groupes
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize]
    public class EmployeController : ControllerBase
    {
        private readonly IEmployeService _employeService;

        public EmployeController(IEmployeService employeService)
        {
            _employeService = employeService;
        }

        /// <summary>
        /// GET api/employe/equipements/{utilisateurId}
        /// Liste plate des affectations (rétrocompatibilité)
        /// </summary>
        [HttpGet("equipements/{utilisateurId}")]
        public async Task<IActionResult> GetEquipementsAffectes(int utilisateurId)
        {
            if (utilisateurId <= 0) return BadRequest("ID utilisateur invalide.");
            var equipements = await _employeService.GetEquipementsAffectesAsync(utilisateurId);
            return Ok(equipements);
        }

        /// <summary>
        /// GET api/employe/equipements/detail/{affectationId}
        /// Détail d'une affectation
        /// </summary>
        [HttpGet("equipements/detail/{affectationId}")]
        public async Task<IActionResult> GetEquipementDetail(int affectationId)
        {
            var equipement = await _employeService.GetEquipementDetailAsync(affectationId);
            if (equipement == null) return NotFound("Affectation introuvable.");
            return Ok(equipement);
        }

        /// <summary>
        /// GET api/employe/{utilisateurId}/materiels-groupes
        /// Retourne les matériels groupés avec leurs articles individuels.
        /// C'est l'endpoint utilisé par la nouvelle grille MesEquipements.
        /// </summary>
        [HttpGet("{utilisateurId}/materiels-groupes")]
        public async Task<IActionResult> GetMaterielsGroupes(int utilisateurId)
        {
            if (utilisateurId <= 0) return BadRequest("ID utilisateur invalide.");
            var groupes = await _employeService.GetMaterielsGroupesAsync(utilisateurId);
            return Ok(groupes);
        }
    }
}