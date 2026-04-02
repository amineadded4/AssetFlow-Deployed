using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IOcrInvoiceService
    {
        // Run Mistral OCR on a single PDF and return full markdown text.
        Task<string> ExtractMarkdownAsync(byte[] pdfBytes, string fileName);
        
        // Send markdown to Llama 4 and return structured invoice data.
        Task<InvoiceOcrDto?> ExtractStructuredDataAsync(string markdownText);
        Task SaveOcrCacheAsync(Guid offreId, InvoiceOcrDto data);
        Task<InvoiceOcrDto?> GetOcrCacheAsync(Guid offreId);
    }
}