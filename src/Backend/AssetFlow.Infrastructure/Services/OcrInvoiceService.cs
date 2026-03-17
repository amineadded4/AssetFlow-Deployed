// ============================================================
// AssetFlow.Infrastructure / Services / OcrInvoiceService.cs
// Single-PDF OCR via Mistral + structured extraction via Gemini
// ============================================================

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AssetFlow.Infrastructure.Services
{
    public class OcrInvoiceService : IOcrInvoiceService
    {
        private readonly HttpClient      _http;
        private readonly IConfiguration _config;

        public OcrInvoiceService(HttpClient http, IConfiguration config)
        {
            _http   = http;
            _config = config;
        }

        // ── 1. Mistral OCR → Markdown ────────────────────────────
        public async Task<string> ExtractMarkdownAsync(byte[] pdfBytes, string fileName)
        {
            var b64        = Convert.ToBase64String(pdfBytes);
            var mistralKey = _config["MistralApiKey"] ?? "";

            var payload = new
            {
                model    = "mistral-ocr-latest",
                document = new
                {
                    type         = "document_url",
                    document_url = $"data:application/pdf;base64,{b64}"
                },
                include_image_base64 = false
            };

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Clone / set header per-request to stay thread-safe
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mistral.ai/v1/ocr")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mistralKey);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc          = JsonDocument.Parse(responseJson);

            // Concatenate all pages into one markdown string
            var sb = new StringBuilder();
            foreach (var page in doc.RootElement.GetProperty("pages").EnumerateArray())
            {
                if (page.TryGetProperty("markdown", out var md))
                    sb.AppendLine(md.GetString());
            }
            return sb.ToString();
        }

        // ── 2. Gemini → structured InvoiceOcrDto ─────────────────
        public async Task<InvoiceOcrDto?> ExtractStructuredDataAsync(string markdownText)
        {
            var geminiKey = _config["GeminiApiKey"] ?? "";

            var prompt = $@"
Tu es un expert en traitement de factures.
Voici le contenu OCR d'une facture au format Markdown.

Extrais TOUTES les informations et retourne UNIQUEMENT un JSON valide,
sans texte autour, sans balises markdown, sans explication.

Structure JSON attendue :
{{
  ""fournisseur"": {{
    ""nom"": """",
    ""adresse"": """",
    ""telephone"": """",
    ""email"": """",
    ""site_web"": """",
    ""tva_intra"": """",
    ""iban"": """",
    ""bic_swift"": """",
    ""banque"": """"
  }},
  ""client"": {{
    ""nom"": """",
    ""adresse"": """"
  }},
  ""facture"": {{
    ""numero"": """",
    ""date"": """",
    ""echeance"": """",
    ""paiement"": """",
    ""reference"": """",
    ""numero_commande"": """"
  }},
  ""informations_additionnelles"": {{
    ""garantie"": """",
    ""delai_livraison"": """",
    ""frais_livraison"": """"
  }},
  ""lignes"": [
    {{
      ""description"": """",
      ""quantite"": """",
      ""unite"": """",
      ""prix_unitaire_ht"": """",
      ""tva_pct"": """",
      ""total_tva"": """",
      ""total_ttc"": """"
    }}
  ],
  ""totaux"": {{
    ""total_ht"": """",
    ""total_tva"": """",
    ""total_ttc"": """"
  }}
}}

Facture (Markdown OCR) :
{markdownText}
";

            var geminiPayload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var json    = JsonSerializer.Serialize(geminiPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url     = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={geminiKey}";

            var response = await _http.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc          = JsonDocument.Parse(responseJson);

            var rawText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            // Strip markdown fences if present
            rawText = rawText.Trim();
            if (rawText.StartsWith("```"))
            {
                var lines = rawText.Split('\n').ToList();
                lines.RemoveAt(0);
                if (lines.Count > 0 && lines.Last().Trim() == "```")
                    lines.RemoveAt(lines.Count - 1);
                rawText = string.Join('\n', lines);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower
            };
            return JsonSerializer.Deserialize<InvoiceOcrDto>(rawText, options);
        }
    }
}