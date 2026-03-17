// ============================================================
// AssetFlow.WebAPI / Controllers / OffreSelectionController.cs
// POST api/offre-selection/confirm   → sauvegarde dans Redis
// GET  api/offre-selection/{userId}  → récupère les sélections
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/offre-selection")]
    [Authorize(Policy = "ITOnly")]
    public class OffreSelectionController : ControllerBase
    {
        private readonly IRedisOffreService _redis;
        private readonly IOffreAchatService _offres;
        private readonly IOcrInvoiceService _ocr;

        public OffreSelectionController(
            IRedisOffreService redis,
            IOffreAchatService offres,
            IOcrInvoiceService ocr)
        {
            _redis  = redis;
            _offres = offres;
            _ocr    = ocr;
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] OffreSelectionDto dto)
        {
            if (dto.OffreId == Guid.Empty)  return BadRequest("offreId requis.");
            if (dto.IdDemande == 0)         return BadRequest("idDemande requis.");

            // 1. Sauvegarder les infos OCR dans SQL
            await _offres.SauvegarderInfosOcrAsync(
                dto.OffreId,
                dto.PrixTotal,
                dto.FraisLivraison,
                dto.DelaiLivraison,
                dto.Garantie);
            // Sauvegarder infos OCR des autres offres analysées
            foreach (var autre in dto.AutresOffres)
            {
                await _offres.SauvegarderInfosOcrAsync(
                    autre.OffreId, autre.PrixTotal, autre.FraisLivraison, autre.DelaiLivraison, autre.Garantie);
            }

            // 2. Marquer EstChoisie = true
            var success = await _offres.ChoisirOffreAsync(dto.OffreId, dto.IdDemande);
            if (!success) return NotFound("Offre introuvable.");

            // 3. Supprimer TOUS les caches OCR de la demande
            var toutesLesOffres = await _offres.GetByDemandeIdAsync(dto.IdDemande);
            foreach (var offre in toutesLesOffres)
                await _redis.DeleteOffreSelectionAsync($"ocr_cache:{offre.IdOffre}");

            return Ok(new { success = true });
        }
    }
}