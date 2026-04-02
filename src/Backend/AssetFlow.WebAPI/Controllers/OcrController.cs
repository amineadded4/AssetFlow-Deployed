using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ITOrAdmin")]
    public class OcrController : ControllerBase
    {
        private readonly IOcrInvoiceService _ocr;
        private readonly IOffreAchatService _offres;

        public OcrController(IOcrInvoiceService ocr, IOffreAchatService offres)
        {
            _ocr    = ocr;
            _offres = offres;
        }

        // POST api/ocr/analyze/{offreId}
        // Fetches the stored PDF from DB for the given offre, runs OCR + Llama 4,
        // and returns the structured InvoiceOcrDto.
        [HttpPost("analyze/{offreId:guid}")]
        public async Task<IActionResult> Analyze(Guid offreId)
        {
            // 1. Vérifier le cache Redis
            var cached = await _ocr.GetOcrCacheAsync(offreId);
            if (cached != null)
                return Ok(cached);  // ← retourne direct sans relancer OCR

            // 2. Charger PDF
            var pdfBytes = await _offres.GetPdfBytesAsync(offreId);
            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound("PDF introuvable.");

            // 3. OCR Mistral
            string markdown;
            try { markdown = await _ocr.ExtractMarkdownAsync(pdfBytes, offreId.ToString()); }
            catch (Exception ex) { return StatusCode(502, $"Erreur OCR Mistral : {ex.Message}"); }

            // 4. Groq → structured data
            InvoiceOcrDto? result;
            try { result = await _ocr.ExtractStructuredDataAsync(markdown); }
            catch (Exception ex) { return StatusCode(502, $"Erreur Groq : {ex.Message}"); }

            if (result == null)
                return StatusCode(500, "Extraction échouée.");

            // 5. Sauvegarder dans Redis cache 24h
            await _ocr.SaveOcrCacheAsync(offreId, result);

            return Ok(result);
        }
        
        // GET api/ocr/cache/{offreId}
        // Retourne le cache Redis SANS lancer l'OCR si absent
        [HttpGet("cache/{offreId:guid}")]
        public async Task<IActionResult> GetCache(Guid offreId)
        {
            var cached = await _ocr.GetOcrCacheAsync(offreId);
            if (cached == null)
                return NoContent(); // 204 = pas de cache, pas d'erreur
            return Ok(cached);
        }
    }
}