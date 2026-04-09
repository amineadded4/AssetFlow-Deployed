using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/audit-logs")]
    [Authorize(Policy = "AdminOnly")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _svc;
        public AuditLogController(IAuditLogService svc) => _svc = svc;

        // GET api/audit-logs?page=1&pageSize=50&dateDebut=...&dateFin=...&action=...&search=...
        [HttpGet]
        public async Task<IActionResult> GetLogs([FromQuery] AuditLogQueryDto query)
            => Ok(await _svc.GetLogsAsync(query));
    }
}