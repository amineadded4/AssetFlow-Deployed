using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ITOrAdmin")]
    public class OffreAchatController : ControllerBase
    {
        private readonly IOffreAchatService _service;

        public OffreAchatController(IOffreAchatService service)
        {
            _service = service;
        }

        // GET api/offreachat/demande/5
        [HttpGet("demande/{demandeId:int}")]
        public async Task<IActionResult> GetByDemande(int demandeId)
        {
            var offres = await _service.GetByDemandeIdAsync(demandeId);
            return Ok(offres);
        }

        // GET api/offreachat/{id}/pdf
        [HttpGet("{id:guid}/pdf")]
        public async Task<IActionResult> GetPdf(Guid id)
        {
            var bytes = await _service.GetPdfBytesAsync(id);
            if (bytes == null || bytes.Length == 0)
                return NotFound("PDF introuvable.");

            return File(bytes, "application/pdf");
        }
    }
}
