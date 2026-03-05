// ── Routes IT (ajouter un second controller ou changer policy) ──
using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AssetFlow.WebAPI.Controllers{
    [ApiController]
    [Route("api/it/incidents")]
    [Authorize(Policy = "ITOnly")]
    public class ITIncidentController : ControllerBase
    {
        private readonly IIncidentService _svc;
        public ITIncidentController(IIncidentService svc) => _svc = svc;

        // GET api/it/incidents/employes?search=...
        [HttpGet("employes")]
        public async Task<IActionResult> GetEmployes([FromQuery] string? search = null)
            => Ok(await _svc.GetEmployesAvecIncidentsAsync(search));

        // GET api/it/incidents/employes/{userId}/materiels
        [HttpGet("employes/{userId:int}/materiels")]
        public async Task<IActionResult> GetMateriels(int userId)
            => Ok(await _svc.GetMaterielsAvecIncidentsAsync(userId));

        // PATCH api/it/incidents/{incidentId}/statut
        [HttpPatch("{incidentId:int}/statut")]
        public async Task<IActionResult> ChangerStatut(int incidentId, [FromBody] ChangerStatutIncidentDto dto)
        {
            var result = await _svc.ChangerStatutAsync(incidentId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // POST api/it/incidents/resolve-all-article
        [HttpPost("resolve-all-article")]
        public async Task<IActionResult> ResolveAll([FromBody] ResolveAllArticleDto dto)
        {
            var result = await _svc.ResolveAllByArticleAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
