// ============================================================
// AssetFlow.WebAPI / Controllers / OcrController.cs
// POST api/ocr/analyze/{offreId}  — analyze ONE offre PDF
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ITOnly")]
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
        // Fetches the stored PDF from DB for the given offre, runs OCR + Gemini,
        // and returns the structured InvoiceOcrDto.
        [HttpPost("analyze/{offreId:guid}")]
        public async Task<IActionResult> Analyze(Guid offreId)
        {
            // 1. Load PDF bytes from DB
            var pdfBytes = await _offres.GetPdfBytesAsync(offreId);
            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound("PDF introuvable pour cette offre.");

            // 2. Mistral OCR → Markdown
            string markdown;
            try
            {
                markdown = await _ocr.ExtractMarkdownAsync(pdfBytes, offreId.ToString());
            }
            catch (Exception ex)
            {
                return StatusCode(502, $"Erreur OCR Mistral : {ex.Message}");
            }

            // 3. Gemini → structured data
            InvoiceOcrDto? result;
            try
            {
                result = await _ocr.ExtractStructuredDataAsync(markdown);
            }
            catch (Exception ex)
            {
                return StatusCode(502, $"Erreur Gemini : {ex.Message}");
            }

            if (result == null)
                return StatusCode(500, "L'extraction structurée n'a retourné aucune donnée.");

            return Ok(result);
        }
    }
}