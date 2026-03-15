// ============================================================
// AssetFlow.WebAPI / Controllers / IncidentController.cs
// MISE À JOUR : Ajout endpoint GET api/incident/affectation/{affectationId}
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class IncidentController : ControllerBase
    {
        private readonly IIncidentService _incidentService;

        public IncidentController(IIncidentService incidentService)
        {
            _incidentService = incidentService;
        }

        /// <summary>
        /// POST api/incident/signaler
        /// Signale un nouvel incident
        /// </summary>
        [HttpPost("signaler")]
        public async Task<IActionResult> SignalerIncident([FromBody] SignalerIncidentRequestDto request)
        {
            if (request.AffectationId <= 0)
                return BadRequest("ID affectation invalide.");

            if (string.IsNullOrWhiteSpace(request.TypeIncident))
                return BadRequest("Type d'incident requis.");

            if (string.IsNullOrWhiteSpace(request.Description))
                return BadRequest("Description requise.");

            var result = await _incidentService.SignalerIncidentAsync(request);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result);
        }

        /// <summary>
        /// GET api/incident/affectation/{affectationId}
        /// NOUVEAU : Récupère tous les incidents liés à une affectation
        /// </summary>
        [HttpGet("affectation/{affectationId}")]
        public async Task<IActionResult> GetIncidentsByAffectation(int affectationId)
        {
            if (affectationId <= 0)
                return BadRequest("ID affectation invalide.");

            var incidents = await _incidentService.GetIncidentsByAffectationAsync(affectationId);
            return Ok(incidents);
        }

        /// <summary>
        /// GET api/incident/{incidentId}
        /// Récupère le détail d'un incident
        /// </summary>
        [HttpGet("{incidentId}")]
        public async Task<IActionResult> GetIncidentDetail(int incidentId)
        {
            var incident = await _incidentService.GetIncidentDetailAsync(incidentId);

            if (incident == null)
                return NotFound("Incident introuvable.");

            return Ok(incident);
        }
    }
}