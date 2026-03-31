// ============================================================
// AssetFlow.Application / Services / ChatOffreService.cs
// ============================================================

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AssetFlow.Infrastructure.Services
{
    public class ChatOffreService : IChatOffreService
    {
        private readonly IRedisOffreService _redis;
        private readonly IConfiguration    _config;
        private readonly HttpClient        _http;

        public ChatOffreService(IRedisOffreService redis, IConfiguration config, HttpClient http)
        {
            _redis  = redis;
            _config = config;
            _http   = http;
        }

        public async Task<(string Reply, string? RecommendedOffre)> SendAsync(ChatOffreRequestDto dto)
        {
            var historyKey = $"chat_offre:{dto.UserId}:{dto.IdDemande}";

            // 1. Charger historique depuis Redis
            var historyJson = await _redis.GetOffreSelectionAsync(historyKey);
            var history = string.IsNullOrEmpty(historyJson)
                ? new List<ChatbotMessageDto>()
                : JsonSerializer.Deserialize<List<ChatbotMessageDto>>(historyJson) ?? new();

            // 2. Construire contexte OCR
            var contexte = string.Join("\n", dto.Offres.Select(o =>
                $"- {o.NomFichier}: Prix={o.PrixTotal ?? "N/A"}, Délai={o.DelaiLivraison ?? "N/A"}, Garantie={o.Garantie ?? "N/A"}, Frais={o.FraisLivraison ?? "N/A"}"));

            var systemPrompt = $@"Tu es un assistant d'aide à la décision pour la sélection d'offres fournisseurs.

Voici les offres disponibles :
{contexte}

RÈGLES DE FORMATAGE OBLIGATOIRES :
- Quand tu mentionnes un nom de fichier PDF, écris-le toujours en MAJUSCULES entre guillemets doubles, ex : ""FACTURE.PDF""
- Structure toujours ta réponse avec des sections claires séparées par des sauts de ligne
- Utilise des tirets pour les listes
- Sois concis : maximum 5 lignes par réponse
- Quand tu recommandes une offre, écris son nom entre [[ ]] ex : [[facture.pdf]] (nom exact, sensible à la casse)
- Ne recommande qu'une seule offre à la fois

Réponds en français.";

            // 3. Construire messages pour Groq
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };
            foreach (var h in history.TakeLast(10))
                messages.Add(new { role = h.Role, content = h.Content });
            messages.Add(new { role = "user", content = dto.Message });

            // 4. Appel Groq
            var payload = new
            {
                model       = "meta-llama/Llama-4-Scout-17B-16E-Instruct",
                messages,
                temperature = 0.7,
                max_tokens  = 500,
                stream      = false
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config["GroqApiKey"]);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc          = JsonDocument.Parse(responseJson);
            var assistantReply = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            // 5. Sauvegarder historique dans Redis (24h)
            history.Add(new ChatbotMessageDto { Role = "user",      Content = dto.Message,    SentAt = DateTime.UtcNow });
            history.Add(new ChatbotMessageDto { Role = "assistant", Content = assistantReply, SentAt = DateTime.UtcNow });
            await _redis.SaveOffreSelectionAsync(historyKey, JsonSerializer.Serialize(history), TimeSpan.FromHours(24));

            // 6. Détecter offre recommandée [[nom.pdf]]
            string? recommended = null;
            var match = System.Text.RegularExpressions.Regex.Match(assistantReply, @"\[\[(.+?)\]\]");
            if (match.Success) recommended = match.Groups[1].Value;

            if (!string.IsNullOrEmpty(recommended))
            {
                var recKey = $"chat_offre_rec:{dto.UserId}:{dto.IdDemande}";
                await _redis.SaveOffreSelectionAsync(recKey, recommended, TimeSpan.FromHours(24));
            }

            return (assistantReply, recommended);
        }

        public async Task<List<ChatbotMessageDto>> GetHistoryAsync(string userId, int idDemande)
        {
            var key  = $"chat_offre:{userId}:{idDemande}";
            var json = await _redis.GetOffreSelectionAsync(key);
            if (string.IsNullOrEmpty(json)) return new();
            return JsonSerializer.Deserialize<List<ChatbotMessageDto>>(json) ?? new();
        }

        public async Task DeleteHistoryAsync(string userId, int idDemande)
        {
            await _redis.DeleteOffreSelectionAsync($"chat_offre:{userId}:{idDemande}");
        }

        public async Task<string?> GetRecommendationAsync(string userId, int idDemande)
        {
            var key = $"chat_offre_rec:{userId}:{idDemande}";
            return await _redis.GetOffreSelectionAsync(key);
        }
    }
}