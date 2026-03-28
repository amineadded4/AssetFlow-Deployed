// ============================================================
// AssetFlow.Infrastructure / Services / SentimentService.cs
// FIX : Mistral retourne { commentaires:[...], statistiques:{...} }
//       → on lit les stats dans le sous-objet "statistiques"
// ============================================================

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssetFlow.Infrastructure.Services
{
    public class SentimentService : ISentimentService
    {
        private readonly AppDbContext              _context;
        private readonly HttpClient                _http;
        private readonly string?                   _apiKey;
        private readonly ILogger<SentimentService> _logger;

        private const string MISTRAL_URL   = "https://api.mistral.ai/v1/chat/completions";
        private const string MISTRAL_MODEL = "ministral-3b-latest";

        public SentimentService(
            AppDbContext context,
            IHttpClientFactory httpFactory,
            IConfiguration config,
            ILogger<SentimentService> logger)
        {
            _context = context;
            _http    = httpFactory.CreateClient("MistralClient");
            _apiKey  = config["Mistral:ApiKey"];
            _logger  = logger;
        }

        // ── Analyse un seul matériel ──────────────────────────────
        public async Task<SentimentMaterielDto> AnalyserSentimentMaterielAsync(int materielId)
        {
            var materiel = await _context.Materiels
                .FirstOrDefaultAsync(m => m.Id == materielId)
                ?? throw new KeyNotFoundException($"Matériel {materielId} introuvable.");

            var commentaires = await _context.CommentairesMateriel
                .Where(c => c.MaterielId == materielId)
                .OrderByDescending(c => c.DateCreation)
                .Take(40)
                .Select(c => new SentimentCommentairePayload { Id = c.Id, Contenu = c.Contenu })
                .ToListAsync();

            if (!commentaires.Any())
            {
                return new SentimentMaterielDto
                {
                    MaterielId        = materielId,
                    MaterielRef       = materiel.Reference,
                    MaterielNom       = materiel.Designation,
                    TotalCommentaires = 0,
                    Resume            = "Aucun commentaire disponible.",
                    SentimentDominant = "Neutre",
                    ScoreGlobal       = 3
                };
            }

            try
            {
                return await AppelerMistralAsync(
                    materielId, materiel.Reference, materiel.Designation, commentaires);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Mistral AI indisponible pour matériel {Id} — fallback algorithmique.", materielId);
                return AnalyseAlgorithmique(
                    materielId, materiel.Reference, materiel.Designation, commentaires);
            }
        }

        // ── Analyse tous les matériels ────────────────────────────
        public async Task<List<SentimentMaterielDto>> AnalyserTousMaterielAsync()
        {
            var materielIds = await _context.CommentairesMateriel
                .Select(c => c.MaterielId)
                .Distinct()
                .ToListAsync();

            var resultats = new List<SentimentMaterielDto>();

            foreach (var id in materielIds)
            {
                try
                {
                    resultats.Add(await AnalyserSentimentMaterielAsync(id));
                    await Task.Delay(1100); // Mistral free tier : 1 req/s
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur analyse sentiment matériel {Id}", id);
                }
            }

            return resultats.OrderByDescending(r => r.TotalCommentaires).ToList();
        }

        // ── Appel Mistral AI ──────────────────────────────────────
        private async Task<SentimentMaterielDto> AppelerMistralAsync(
            int materielId,
            string reference,
            string designation,
            List<SentimentCommentairePayload> commentaires)
        {
            if (string.IsNullOrEmpty(_apiKey))
                throw new InvalidOperationException(
                    "Clé Mistral manquante. Ajouter 'Mistral:ApiKey' dans appsettings.Development.json.");

            var total = commentaires.Count;
            var texteCommentaires = string.Join("\n",
                commentaires.Select((c, i) => $"{i + 1}. [id:{c.Id}] {c.Contenu}"));

            _logger.LogInformation("Mistral — analyse {Count} commentaires pour matériel {Id}",
                total, materielId);

            var systemPrompt = """
                Tu es un expert en analyse de sentiment pour des équipements informatiques en entreprise.
                Tu réponds TOUJOURS et UNIQUEMENT avec un objet JSON valide.
                Tu n'ajoutes JAMAIS de texte avant ou après le JSON.
                Tu n'utilises JAMAIS de blocs markdown (pas de ``` ni de ```json).
                """;

            // ── IMPORTANT : on demande le format PLAT directement ──
            var userPrompt = $@"
Matériel analysé : {designation} (Réf: {reference})
Nombre de commentaires : {total}

Commentaires :
{texteCommentaires}

Analyse chaque commentaire et retourne UNIQUEMENT ce JSON plat (sans sous-objets) :
{{""positifs"":{0},""negatifs"":{0},""neutres"":{0},""score"":3.0,""dominant"":""Neutre"",""resume"":""synthèse courte""}}

Règles :
- positifs + negatifs + neutres doit être égal à {total}
- score entre 1.0 et 5.0
- dominant = ""Positif"", ""Négatif"", ""Mitigé"" ou ""Neutre""
- Il est INTERDIT de retourner 0 pour tous les champs si les commentaires expriment une opinion
";

            var requestBody = new
            {
                model    = MISTRAL_MODEL,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = userPrompt   }
                },
                max_tokens      = 300,
                temperature     = 0.1,
                response_format = new { type = "json_object" }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, MISTRAL_URL);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response     = await _http.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Mistral réponse brute : {Body}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Mistral AI erreur {Status} : {Body}",
                    response.StatusCode, responseBody);
                throw new HttpRequestException($"Mistral HTTP {response.StatusCode}: {responseBody}");
            }

            return ParseReponse(responseBody, materielId, reference, designation, total);
        }

        // ── Parser la réponse (format OpenAI-compatible) ──────────
        private SentimentMaterielDto ParseReponse(
            string responseBody,
            int    materielId,
            string reference,
            string designation,
            int    total)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);

                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                // Nettoyer d'éventuels backticks résiduels
                content = content.Trim();
                if (content.StartsWith("```"))
                {
                    var firstBrace = content.IndexOf('{');
                    var lastBrace  = content.LastIndexOf('}');
                    if (firstBrace >= 0 && lastBrace > firstBrace)
                        content = content[firstBrace..(lastBrace + 1)];
                }

                // Extraire le JSON entre { et }
                var start = content.IndexOf('{');
                var end   = content.LastIndexOf('}');
                if (start >= 0 && end > start)
                    content = content[start..(end + 1)];

                using var sentDoc = JsonDocument.Parse(content);
                var root = sentDoc.RootElement;

                // ════════════════════════════════════════════════════
                // FIX PRINCIPAL : Mistral peut retourner deux formats
                //   Format A (attendu)  : { "positifs":3, "negatifs":0, ... }
                //   Format B (observé)  : { "commentaires":[...], "statistiques":{ "positifs":3, ... } }
                // On détecte le format B et on lit dans le sous-objet.
                // ════════════════════════════════════════════════════
                JsonElement statsEl = root;

                if (root.TryGetProperty("statistiques", out var statsSubObj) &&
                    statsSubObj.ValueKind == JsonValueKind.Object)
                {
                    statsEl = statsSubObj;
                    _logger.LogInformation(
                        "ParseReponse : format imbriqué détecté → lecture dans 'statistiques'");
                }

                int positifs = GetInt(statsEl, "positifs");
                int negatifs = GetInt(statsEl, "negatifs");
                int neutres  = GetInt(statsEl, "neutres");

                // Normaliser si la somme ne correspond pas
                int somme = positifs + negatifs + neutres;
                if (somme != total && somme > 0)
                {
                    double f = (double)total / somme;
                    positifs = (int)Math.Round(positifs * f);
                    negatifs = (int)Math.Round(negatifs * f);
                    neutres  = total - positifs - negatifs;
                    if (neutres < 0) neutres = 0;
                }
                else if (somme == 0)
                {
                    neutres = total;
                }

                double score    = Math.Clamp(GetDouble(statsEl, "score"), 1.0, 5.0);
                string dominant = NormaliserDominant(GetString(statsEl, "dominant", "Neutre"));
                string resume   = GetString(statsEl, "resume", "");
                if (resume.Length > 150) resume = resume[..150];

                _logger.LogInformation(
                    "Sentiment parsé — positifs:{P} negatifs:{N} neutres:{U} dominant:{D}",
                    positifs, negatifs, neutres, dominant);

                return new SentimentMaterielDto
                {
                    MaterielId         = materielId,
                    MaterielRef        = reference,
                    MaterielNom        = designation,
                    TotalCommentaires  = total,
                    Positifs           = positifs,
                    Negatifs           = negatifs,
                    Neutres            = neutres,
                    PourcentagePositif = total > 0 ? Math.Round((double)positifs / total * 100, 1) : 0,
                    PourcentageNegatif = total > 0 ? Math.Round((double)negatifs / total * 100, 1) : 0,
                    PourcentageNeutre  = total > 0 ? Math.Round((double)neutres  / total * 100, 1) : 0,
                    ScoreGlobal        = Math.Round(score, 1),
                    SentimentDominant  = dominant,
                    Resume             = resume
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Parsing réponse Mistral échoué — fallback algorithmique.");
                throw;
            }
        }

        // ── Fallback algorithmique (sans API) ─────────────────────
        private static SentimentMaterielDto AnalyseAlgorithmique(
            int materielId,
            string reference,
            string designation,
            List<SentimentCommentairePayload> commentaires)
        {
            var motsPositifs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "bien","bon","bonne","excellent","excellente","parfait","parfaite",
                "super","top","génial","rapide","fiable","efficace","pratique",
                "agréable","satisfait","satisfaite","content","contente","bravo",
                "qualité","solide","robuste","performant","facile","utile",
                "recommande","recommandé","apprécié","apprécie","j'adore",
                "aime","aimé","positif","merveilleux","formidable","nickel"
            };

            var motsNegatifs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "mauvais","mauvaise","nul","nulle","médiocre","décevant","décevante",
                "problème","problèmes","bug","bugs","lent","lente","cassé","cassée",
                "défaut","défauts","panne","pannes","inutile","fragile","bruyant",
                "cher","difficile","compliqué","insatisfait","insatisfaite",
                "déçu","déçue","déception","horrible","regrette","éviter","déconseille"
            };

            int positifs = 0, negatifs = 0, neutres = 0;

            foreach (var c in commentaires)
            {
                var mots = c.Contenu
                    .ToLower()
                    .Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '\n', '\r', '"', '\'', '-' },
                           StringSplitOptions.RemoveEmptyEntries);

                int scorePos = mots.Count(m => motsPositifs.Contains(m));
                int scoreNeg = mots.Count(m => motsNegatifs.Contains(m));

                if      (scorePos > scoreNeg) positifs++;
                else if (scoreNeg > scorePos) negatifs++;
                else                          neutres++;
            }

            int    total   = commentaires.Count;
            double pctPos  = total > 0 ? Math.Round((double)positifs / total * 100, 1) : 0;
            double pctNeg  = total > 0 ? Math.Round((double)negatifs / total * 100, 1) : 0;
            double pctNeu  = total > 0 ? Math.Round((double)neutres  / total * 100, 1) : 0;
            double score   = total > 0 ? Math.Round(1.0 + (double)positifs / total * 4.0, 1) : 3.0;

            string dominant = "Neutre";
            if      (pctPos > 60) dominant = "Positif";
            else if (pctNeg > 60) dominant = "Négatif";
            else if (pctPos > 0 && pctNeg > 0) dominant = "Mitigé";

            string resume = dominant switch
            {
                "Positif" => $"Les utilisateurs sont globalement satisfaits de {designation}.",
                "Négatif" => $"Les utilisateurs expriment plusieurs insatisfactions sur {designation}.",
                "Mitigé"  => $"Les avis sont partagés sur {designation}.",
                _         => $"Les commentaires sur {designation} restent neutres ou factuels."
            };

            return new SentimentMaterielDto
            {
                MaterielId         = materielId,
                MaterielRef        = reference,
                MaterielNom        = designation,
                TotalCommentaires  = total,
                Positifs           = positifs,
                Negatifs           = negatifs,
                Neutres            = neutres,
                PourcentagePositif = pctPos,
                PourcentageNegatif = pctNeg,
                PourcentageNeutre  = pctNeu,
                ScoreGlobal        = score,
                SentimentDominant  = dominant,
                Resume             = resume + " (analyse locale)"
            };
        }

        // ── Normaliser le sentiment dominant ──────────────────────
        private static string NormaliserDominant(string raw) => raw.Trim() switch
        {
            var s when s.Contains("Positif", StringComparison.OrdinalIgnoreCase) => "Positif",
            var s when s.Contains("Négatif", StringComparison.OrdinalIgnoreCase) => "Négatif",
            var s when s.Contains("Negatif", StringComparison.OrdinalIgnoreCase) => "Négatif",
            var s when s.Contains("Mitigé",  StringComparison.OrdinalIgnoreCase) => "Mitigé",
            var s when s.Contains("Mixte",   StringComparison.OrdinalIgnoreCase) => "Mitigé",
            var s when s.Contains("Mixed",   StringComparison.OrdinalIgnoreCase) => "Mitigé",
            _ => "Neutre"
        };

        // ── Helpers JSON sûrs ─────────────────────────────────────
        private static int GetInt(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var v))
            {
                if (v.ValueKind == JsonValueKind.Number) return v.GetInt32();
                if (int.TryParse(v.GetString(), out var i)) return i;
            }
            return 0;
        }

        private static double GetDouble(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var v))
            {
                if (v.ValueKind == JsonValueKind.Number) return v.GetDouble();
                if (double.TryParse(v.GetString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var d)) return d;
            }
            return 3.0;
        }

        private static string GetString(JsonElement el, string prop, string fallback = "")
        {
            if (el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString() ?? fallback;
            return fallback;
        }
    }
}
