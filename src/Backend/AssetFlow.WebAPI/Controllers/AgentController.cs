// src/Backend/AssetFlow.WebAPI/Controllers/AgentController.cs
// ─────────────────────────────────────────────────────────────────────────────
// MODIFIÉ — Ajout de 3 endpoints pour le workflow "Demande d'achat" :
//   GET  /api/agent/demandes-pending
//   POST /api/agent/demande/{id}/start
//   POST /api/agent/demande/{id}/select-offer
// ─────────────────────────────────────────────────────────────────────────────
using AssetFlow.Application.DTOs.AgentDtos;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/agent")]
    [Authorize]
    public class AgentController : ControllerBase
    {
        private readonly IAgentService _agent;

        public AgentController(IAgentService agent)
        {
            _agent = agent;
        }

        /// <summary>Alertes initiales à l'ouverture du chat</summary>
        [HttpGet("alerts")]
        public async Task<IActionResult> GetInitialAlerts()
        {
            var result = await _agent.GetInitialAlertsAsync();
            return Ok(result);
        }

        /// <summary>Envoyer un message à l'agent</summary>
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AgentChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Message vide.");

            var result = await _agent.ProcessMessageAsync(request);
            return Ok(result);
        }

        /// <summary>Approuver ou refuser une action proposée par l'agent</summary>
        [HttpPost("approve")]
        public async Task<IActionResult> Approve([FromBody] AgentApprovalRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Utilisateur))
                request.Utilisateur = Request.Headers["X-User-Name"].FirstOrDefault() ?? "Agent IA";

            var result = await _agent.ApproveActionAsync(request);
            return result.Succes ? Ok(result) : BadRequest(result);
        }

        // ════════════════════════════════════════════════════════════════════
        //  ── NOUVEAU : Workflow Demande d'achat ─────────────────────────────
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Liste des demandes d'achat à traiter (statut ≠ traite/commande/refuse)</summary>
        [HttpGet("demandes-pending")]
        public async Task<IActionResult> GetPendingDemandes()
        {
            var result = await _agent.GetPendingDemandesAsync();
            return Ok(result);
        }

        /// <summary>Étape 1 — Lance la recherche web pour une demande d'achat (renvoie 5 offres)</summary>
        [HttpPost("demande/{id:int}/start")]
        public async Task<IActionResult> StartDemandeWorkflow(int id)
        {
            var result = await _agent.StartDemandeWorkflowAsync(id);
            return Ok(result);
        }

        /// <summary>Étape 2 — Une offre est choisie : pré-remplit le formulaire matériel + commande</summary>
        [HttpPost("demande/{id:int}/select-offer")]
        public async Task<IActionResult> SelectOffer(int id, [FromBody] SelectOfferRequest request)
        {
            if (request?.Offre == null)
                return BadRequest("Offre manquante.");

            var result = await _agent.SelectOfferAsync(id, request.Offre);
            return Ok(result);
        }
    }
}
