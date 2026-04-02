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
        /// POST api/incident/signaler
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

        // GET api/incident/affectation/{affectationId}
        // Récupère tous les incidents liés à une affectation
        [HttpGet("affectation/{affectationId}")]
        public async Task<IActionResult> GetIncidentsByAffectation(int affectationId)
        {
            if (affectationId <= 0)
                return BadRequest("ID affectation invalide.");

            var incidents = await _incidentService.GetIncidentsByAffectationAsync(affectationId);
            return Ok(incidents);
        }

        /// GET api/incident/{incidentId}
        /// Récupère le détail d'un incident
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