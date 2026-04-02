using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/statistiques")]
    [Authorize(Policy = "AchatOrAdmin")]
    public class StatistiquesController : ControllerBase
    {
        private readonly IStatistiquesService _svc;
        public StatistiquesController(IStatistiquesService svc) => _svc = svc;

        /// <summary>
        /// Retourne toutes les données du tableau de bord.
        /// annee     : année cible (défaut = année courante)
        /// moisDebut : mois de début 1-12 (défaut = 1)
        /// moisFin   : mois de fin   1-12 (défaut = 12)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] int?  annee     = null,
            [FromQuery] int?  moisDebut = null,
            [FromQuery] int?  moisFin   = null)
        {
            var a  = annee     ?? DateTime.UtcNow.Year;
            var md = moisDebut ?? 1;
            var mf = moisFin   ?? 12;

            if (a < 2000 || a > 2100)
                return BadRequest(new { Message = "Année invalide." });
            if (md < 1 || md > 12 || mf < 1 || mf > 12 || md > mf)
                return BadRequest(new { Message = "Plage de mois invalide." });

            var stats = await _svc.GetDashboardAsync(a, md, mf);
            return Ok(stats);
        }
    }
}
