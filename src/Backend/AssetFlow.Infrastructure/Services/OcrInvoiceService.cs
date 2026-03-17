// ============================================================
// AssetFlow.Infrastructure / Services / OcrInvoiceService.cs
// Single-PDF OCR via Mistral + structured extraction via Groq (Llama 4)
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

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mistral.ai/v1/ocr")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mistralKey);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc          = JsonDocument.Parse(responseJson);

            var sb = new StringBuilder();
            foreach (var page in doc.RootElement.GetProperty("pages").EnumerateArray())
            {
                if (page.TryGetProperty("markdown", out var md))
                    sb.AppendLine(md.GetString());
            }
            return sb.ToString();
        }

        // ── 2. Llama 4 (via Groq API) → structured InvoiceOcrDto ─────────────────
        public async Task<InvoiceOcrDto?> ExtractStructuredDataAsync(string markdownText)
        {
            var groqKey = _config["GroqApiKey"] ?? "";

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

            var payload = new
            {
                model = "meta-llama/Llama-4-Scout-17B-16E-Instruct",
                messages = new[]
                {
                    new { role = "system", content = "You are a precise invoice data extraction assistant. Return only valid JSON without any explanation or markdown formatting." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.1,
                max_completion_tokens = 4096,  // Note: max_completion_tokens (pas max_tokens)
                top_p = 1,
                stop = (string?)null,
                stream = false,  // Désactivé car on veut une réponse complète
                response_format = new { type = "json_object" }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Changement de l'URL pour Groq API
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", groqKey);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseJson);
            
            var rawText = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            // Nettoyage du texte
            rawText = rawText.Trim();
            if (rawText.StartsWith("```json"))
                rawText = rawText.Replace("```json", "").Replace("```", "").Trim();
            else if (rawText.StartsWith("```"))
                rawText = rawText.Replace("```", "").Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            
            return JsonSerializer.Deserialize<InvoiceOcrDto>(rawText, options);
        }
    }
}