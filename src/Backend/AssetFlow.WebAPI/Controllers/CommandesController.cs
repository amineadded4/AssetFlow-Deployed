// ============================================================
// AssetFlow.WebAPI / Controllers / CommandesController.cs
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/commandes")]
    public class CommandesController : ControllerBase
    {
        private readonly ICommandeService _svc;
        public CommandesController(ICommandeService svc) => _svc = svc;

        // GET api/commandes
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _svc.GetAllAsync());

        // GET api/commandes/materiel/{materielId}
        [HttpGet("materiel/{materielId:int}")]
        public async Task<IActionResult> GetByMateriel(int materielId)
            => Ok(await _svc.GetByMaterielAsync(materielId));

        // GET api/commandes/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _svc.GetByIdAsync(id);
            return c is null ? NotFound() : Ok(c);
        }

        // GET api/commandes/materiels-enrichis
        [HttpGet("materiels-enrichis")]
        public async Task<IActionResult> GetMaterielsEnrichis()
            => Ok(await _svc.GetMaterielsAvecDerniereCommandeAsync());

        // GET api/commandes/articles/{materielId}
        [HttpGet("articles/{materielId:int}")]
        public async Task<IActionResult> GetArticles(int materielId)
            => Ok(await _svc.GetArticlesByMaterielAsync(materielId));

        // POST api/commandes
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreerCommandeDto dto)
        {
            var result = await _svc.CreerAsync(dto);
            return result.Succes ? Ok(result) : BadRequest(result);
        }

        // DELETE api/commandes/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _svc.SupprimerAsync(id);
            return result.Succes ? Ok(result) : BadRequest(result);
        }
    }
}