using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class GraphController : ControllerBase
    {
        private readonly IGraphService _graphService;

        public GraphController(IGraphService graphService)
        {
            _graphService = graphService;
        }

        /// <summary>Stats globales du parc</summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(GraphStatsDto), 200)]
        public async Task<IActionResult> GetStats()
        {
            try { return Ok(await _graphService.GetStatsAsync()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Listes pour le panneau gauche</summary>
        [HttpGet("entities/materiels")]
        public async Task<IActionResult> GetMateriels()
        {
            try { return Ok(await _graphService.GetMaterielsAsync()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("entities/utilisateurs")]
        public async Task<IActionResult> GetUtilisateurs()
        {
            try { return Ok(await _graphService.GetUtilisateursAsync()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("entities/demandes")]
        public async Task<IActionResult> GetDemandes()
        {
            try { return Ok(await _graphService.GetDemandesAsync()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("entities/projets")]
        public async Task<IActionResult> GetProjets()
        {
            try { return Ok(await _graphService.GetProjetsAsync()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Graphe contextuel d'un matériel</summary>
        [HttpGet("context/materiel/{id:int}")]
        [ProducesResponseType(typeof(GraphResponseDto), 200)]
        public async Task<IActionResult> GetGraphMateriel(int id)
        {
            try { return Ok(await _graphService.GetGraphForMaterielAsync(id)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Graphe contextuel d'un utilisateur</summary>
        [HttpGet("context/utilisateur/{id:int}")]
        [ProducesResponseType(typeof(GraphResponseDto), 200)]
        public async Task<IActionResult> GetGraphUtilisateur(int id)
        {
            try { return Ok(await _graphService.GetGraphForUtilisateurAsync(id)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Graphe contextuel d'une demande d'achat</summary>
        [HttpGet("context/demande/{id:int}")]
        [ProducesResponseType(typeof(GraphResponseDto), 200)]
        public async Task<IActionResult> GetGraphDemande(int id)
        {
            try { return Ok(await _graphService.GetGraphForDemandeAsync(id)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Graphe contextuel d'un projet</summary>
        [HttpGet("context/projet/{id:int}")]
        [ProducesResponseType(typeof(GraphResponseDto), 200)]
        public async Task<IActionResult> GetGraphProjet(int id)
        {
            try { return Ok(await _graphService.GetGraphForProjetAsync(id)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Legacy endpoints
        [HttpGet]
        [ProducesResponseType(typeof(GraphResponseDto), 200)]
        public async Task<IActionResult> GetGraph()
        {
            try { return Ok(await _graphService.GetGraphAsync()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("insight/{nodeId}")]
        public async Task<IActionResult> GetNodeInsight(string nodeId)
        {
            var insight = await _graphService.GetInsightForNodeAsync(nodeId);
            if (insight == null) return NotFound();
            return Ok(insight);
        }
    }
}