using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/employes")]
    [Authorize(Policy = "ITOrAdmin")]
    public class EmployesController : ControllerBase
    {
        private readonly IEmployeManagementService _svc;
        public EmployesController(IEmployeManagementService svc) => _svc = svc;

        // GET api/employes?search=...
        [HttpGet]
        public async Task<IActionResult> GetEmployes([FromQuery] string? search = null)
            => Ok(await _svc.GetEmployesAsync(search));

        // GET api/employes/{id}/affectations
        [HttpGet("{id:int}/affectations")]
        public async Task<IActionResult> GetAffectations(int id)
            => Ok(await _svc.GetAffectationsEmployeAsync(id));

        // DELETE api/employes/affectations/{affectationId}
        [HttpDelete("affectations/{affectationId:int}")]
        public async Task<IActionResult> RetirerAffectation(int affectationId)
        {
            var userName  = Request.Headers["X-User-Name"].FirstOrDefault() ?? "Inconnu";
            var result = await _svc.RetirerAffectationAsync(userName, affectationId);
            return result.Succes ? Ok(result) : BadRequest(result);
        }
    }
}