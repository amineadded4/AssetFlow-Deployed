// ============================================================
// AssetFlow.WebAPI / Controllers / CommandesController.cs — v4
// Ajout : PUT /{id} pour modifier une commande
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/commandes")]
    [Authorize(Policy = "EquipeAchatOnly")]
    public class CommandesController : ControllerBase
    {
        private readonly ICommandeService _svc;
        public CommandesController(ICommandeService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _svc.GetAllAsync());

        [HttpGet("materiel/{materielId:int}")]
        public async Task<IActionResult> GetByMateriel(int materielId)
            => Ok(await _svc.GetByMaterielAsync(materielId));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _svc.GetByIdAsync(id);
            return c is null ? NotFound() : Ok(c);
        }

        /// <summary>UNE LIGNE PAR MATERIEL avec commandes imbriquées</summary>
        [HttpGet("lignes-materiels")]
        public async Task<IActionResult> GetLignesMateriels()
            => Ok(await _svc.GetLignesMaterielsAsync());

        /// <summary>Compatibilité : une ligne par commande</summary>
        [HttpGet("lignes-commandes")]
        public async Task<IActionResult> GetLignesCommandes()
            => Ok(await _svc.GetLignesCommandesAsync());

        [HttpGet("articles/{materielId:int}")]
        public async Task<IActionResult> GetArticlesByMateriel(int materielId)
            => Ok(await _svc.GetArticlesByMaterielAsync(materielId));

        [HttpGet("{commandeId:int}/articles")]
        public async Task<IActionResult> GetArticlesByCommande(int commandeId)
            => Ok(await _svc.GetArticlesByCommandeAsync(commandeId));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreerCommandeDto dto)
        {
            var result = await _svc.CreerAsync(dto);
            return result.Succes ? Ok(result) : BadRequest(result);
        }

        /// <summary>Modifier N°commande, fournisseur, dates (sans toucher à la quantité)</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ModifierCommandeDto dto)
        {
            dto.Id = id;
            var result = await _svc.ModifierAsync(dto);
            return result.Succes ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _svc.SupprimerAsync(id);
            return result.Succes ? Ok(result) : BadRequest(result);
        }
    }
}