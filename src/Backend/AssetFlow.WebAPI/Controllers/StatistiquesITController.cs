using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/statistiques-it")]
    [Authorize(Policy = "ITOrAdmin")]
    public class StatistiquesITController : ControllerBase
    {
        private readonly IStatistiquesITService _svc;

        public StatistiquesITController(IStatistiquesITService svc) => _svc = svc;

        /// <summary>
        /// Retourne toutes les données du tableau de bord IT.
        /// Aucun paramètre requis — la période est fixée à 12 semaines côté service.
        /// Le filtrage temporel fin est réalisé côté client (Blazor).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var stats = await _svc.GetDashboardITAsync();
            return Ok(stats);
        }
    }
}
