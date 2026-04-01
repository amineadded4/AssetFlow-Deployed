// ============================================================
// AssetFlow.WebAPI / Controllers / ProjectAffectationsController.cs
// ============================================================

using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/projects")]
    [Authorize(Policy = "ITOrAdmin")]
    public class ProjectAffectationsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectAffectationsController(IProjectService projectService)
            => _projectService = projectService;

        // GET api/projects/{id}/affectations
        [HttpGet("{id:int}/affectations")]
        public async Task<IActionResult> GetAffectations(int id)
        {
            var affectations = await _projectService.GetAffectationsAsync(id);
            return Ok(affectations);
        }
    }
}