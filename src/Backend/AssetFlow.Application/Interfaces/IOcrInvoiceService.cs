// ============================================================
// AssetFlow.Application / Interfaces / IOcrInvoiceService.cs
// ============================================================

using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IOcrInvoiceService
    {
        /// <summary>Run Mistral OCR on a single PDF and return full markdown text.</summary>
        Task<string> ExtractMarkdownAsync(byte[] pdfBytes, string fileName);

        /// <summary>Send markdown to Gemini and return structured invoice data.</summary>
        Task<InvoiceOcrDto?> ExtractStructuredDataAsync(string markdownText);
    }
}