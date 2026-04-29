using AssetFlow.Application.DTOs.AgentDtos;
using AssetFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssetFlow.Infrastructure.Services
{
    public class WebSearchAgentService : IWebSearchAgentService
    {
        private readonly IHttpClientFactory _factory;
        private readonly IConfiguration     _config;

        public WebSearchAgentService(IHttpClientFactory factory, IConfiguration config)
        {
            _factory = factory;
            _config  = config;
        }

        public async Task<string> SearchAsync(string query, List<AgentChatHistory>? history = null)
        {
            var tavilyKey = _config["Tavily:ApiKey"];
            if (string.IsNullOrWhiteSpace(tavilyKey))
                return "❌ Clé API Tavily non configurée.";

            try
            {
                var resolvedQuery = await ResolveQueryWithContextAsync(query, history);

                var http    = _factory.CreateClient();
                var payload = new
                {
                    api_key             = tavilyKey,
                    query               = resolvedQuery,
                    search_depth        = "basic",
                    include_answer      = true,
                    include_raw_content = false,
                    max_results         = 5,
                    include_domains     = Array.Empty<string>(),
                    exclude_domains     = Array.Empty<string>()
                };

                var tavilyResp = await http.PostAsJsonAsync(
                    "https://api.tavily.com/search", payload);

                if (!tavilyResp.IsSuccessStatusCode)
                    return $"❌ Erreur Tavily : {tavilyResp.StatusCode}";

                var tavilyJson = await tavilyResp.Content.ReadAsStringAsync();
                using var doc  = JsonDocument.Parse(tavilyJson);
                var root        = doc.RootElement;

                var answer = root.TryGetProperty("answer", out var ans)
                    ? ans.GetString() ?? ""
                    : "";

                var sources = new List<(string title, string url, string snippet)>();
                if (root.TryGetProperty("results", out var results))
                {
                    foreach (var r in results.EnumerateArray())
                    {
                        var title   = r.TryGetProperty("title",   out var t) ? t.GetString() ?? "" : "";
                        var url     = r.TryGetProperty("url",     out var u) ? u.GetString() ?? "" : "";
                        var snippet = r.TryGetProperty("content", out var c)
                            ? (c.GetString() ?? "")[..Math.Min(200, (c.GetString() ?? "").Length)]
                            : "";
                        if (!string.IsNullOrEmpty(url))
                            sources.Add((title, url, snippet));
                    }
                }

                var groqKey    = _config["GroqApiKey"];
                var mistralKey = _config["MistralApiKey"];

                var sourcesText = string.Join("\n", sources.Select((s, i) =>
                    $"[{i + 1}] {s.title}\nURL: {s.url}\nExtrait: {s.snippet}"));

                var systemPrompt = @"Tu es l'assistant IA d'AssetFlow, un système de gestion de stock.
Réponds en français de manière concise et utile.
Utilise le contexte de la conversation pour comprendre les références implicites (""le meilleur"", ""celui-là"", ""compare-les"", etc.).
Quand tu cites une information tirée d'une source, indique le numéro de la source entre crochets [1], [2], etc.
À la fin de ta réponse, liste TOUJOURS les sources utilisées sous forme de liens cliquables Markdown.
Format des sources : [Titre de la page](URL)";

                var historyContext = BuildHistoryForLlm(history);
                var userPrompt = $@"{historyContext}Question actuelle : {query}

Requête de recherche utilisée : {resolvedQuery}
Réponse directe disponible : {answer}

Sources trouvées :
{sourcesText}

Réponds en synthétisant ces informations. Cite les sources avec [1], [2], etc. 
Termine par une section '## Sources' avec les liens.";

                string synthesizedAnswer;

                if (!string.IsNullOrWhiteSpace(groqKey))
                    synthesizedAnswer = await CallGroqAsync(groqKey, systemPrompt, userPrompt);
                else if (!string.IsNullOrWhiteSpace(mistralKey))
                    synthesizedAnswer = await CallMistralAsync(mistralKey, systemPrompt, userPrompt);
                else
                {
                    var sb = new StringBuilder();
                    if (!string.IsNullOrEmpty(answer)) sb.AppendLine(answer).AppendLine();
                    if (sources.Any())
                    {
                        sb.AppendLine("## Sources");
                        foreach (var (title, url, snippet) in sources)
                        {
                            sb.AppendLine($"- [{title}]({url})");
                            if (!string.IsNullOrEmpty(snippet))
                                sb.AppendLine($"  *{snippet.TrimEnd()}...*");
                        }
                    }
                    return sb.ToString();
                }

                if (!synthesizedAnswer.Contains("## Sources") && sources.Any())
                {
                    var sb = new StringBuilder(synthesizedAnswer);
                    sb.AppendLine("\n## Sources");
                    foreach (var (title, url, _) in sources)
                        sb.AppendLine($"- [{title}]({url})");
                    return sb.ToString();
                }

                return synthesizedAnswer;
            }
            catch (Exception ex)
            {
                return $"❌ Erreur lors de la recherche : {ex.Message}";
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  ── MODIFIÉ : SearchOffersAsync — recherche structurée 4 offres ──
        // ════════════════════════════════════════════════════════════════════
        public async Task<List<OffreSearchResultDto>> SearchOffersAsync(
            string nomProduit, int quantite, string? description = null)
        {
            var tavilyKey = _config["Tavily:ApiKey"];
            var groqKey   = _config["GroqApiKey"];

            // Si aucune clé : renvoie 4 offres "placeholder" claires (jamais d'erreur silencieuse)
            if (string.IsNullOrWhiteSpace(tavilyKey) || string.IsNullOrWhiteSpace(groqKey))
            {
                return BuildFallbackOffers(nomProduit, quantite,
                    "⚠️ Clé Tavily ou Groq manquante — offres exemples affichées.");
            }

            try
            {
                // 1) Tavily : on cible explicitement les fournisseurs / e-commerce
                var http  = _factory.CreateClient();
                var query = $"{nomProduit} prix fournisseur achat professionnel {DateTime.UtcNow.Year}";
                if (!string.IsNullOrWhiteSpace(description))
                    query += $" {description}";

                var payload = new
                {
                    api_key             = tavilyKey,
                    query               = query,
                    search_depth        = "advanced",
                    include_answer      = false,
                    include_raw_content = false,
                    max_results         = 8
                };

                var tavilyResp = await http.PostAsJsonAsync("https://api.tavily.com/search", payload);
                if (!tavilyResp.IsSuccessStatusCode)
                    return BuildFallbackOffers(nomProduit, quantite,
                        $"⚠️ Tavily a renvoyé {tavilyResp.StatusCode} — offres exemples.");

                var tavilyJson = await tavilyResp.Content.ReadAsStringAsync();
                using var doc  = JsonDocument.Parse(tavilyJson);

                var sources = new List<object>();
                if (doc.RootElement.TryGetProperty("results", out var results))
                {
                    int i = 0;
                    foreach (var r in results.EnumerateArray())
                    {
                        if (++i > 8) break;
                        sources.Add(new
                        {
                            title   = r.TryGetProperty("title",   out var t) ? t.GetString() : "",
                            url     = r.TryGetProperty("url",     out var u) ? u.GetString() : "",
                            content = r.TryGetProperty("content", out var c)
                                ? (c.GetString() ?? "")[..Math.Min(400, (c.GetString() ?? "").Length)]
                                : ""
                        });
                    }
                }

                if (sources.Count == 0)
                    return BuildFallbackOffers(nomProduit, quantite,
                        "⚠️ Aucun résultat web pertinent — offres exemples.");

                // 2) Groq : extrait 4 offres structurées en JSON strict
                var llmHttp = _factory.CreateClient();
                llmHttp.DefaultRequestHeaders.Add("Authorization", $"Bearer {groqKey}");

                var sourcesJson = JsonSerializer.Serialize(sources);

                var prompt = $@"Tu es un acheteur professionnel. À partir des résultats web ci-dessous,
extrait EXACTEMENT 4 offres pour le produit suivant :

Produit : {nomProduit}
Quantité demandée : {quantite}
{(string.IsNullOrWhiteSpace(description) ? "" : $"Description : {description}")}

Résultats web (JSON) :
{sourcesJson}

Réponds UNIQUEMENT avec un tableau JSON de 4 objets (sans markdown, sans texte autour) :
[
  {{
    ""fournisseur"":     ""Nom fournisseur (déduit du domaine ou du titre)"",
    ""nomProduit"":      ""Nom exact ou très proche du produit demandé"",
    ""description"":     ""Courte description (1 phrase max)"",
    ""prixUnitaire"":    ""Prix unitaire ex. '4 200 MAD' (si trouvé sinon 'N/A')"",
    ""prixTotal"":       ""Prix total pour {quantite} unités si calculable, sinon 'N/A'"",
    ""fraisLivraison"":  ""ex. 'Gratuit', '150 MAD', 'N/A'"",
    ""delaiLivraison"":  ""ex. '3 à 5 jours', '2 semaines', 'N/A'"",
    ""garantie"":        ""ex. '2 ans constructeur', '1 an', 'N/A'"",
    ""url"":             ""URL de la source"",
    ""devise"":          ""MAD"",
    ""pointsForts"":     [""Atout 1 court"", ""Atout 2 court"", ""Atout 3 court""]
  }},
  ... (4 au total)
]

Si une info n'est PAS trouvée, mets ""N/A"" plutôt qu'inventer. Pour pointsForts, déduis du contexte
(garantie, livraison rapide, prix bas, fournisseur reconnu, etc.). Toujours en français.";

                var llmPayload = new
                {
                    model       = "llama-3.3-70b-versatile",
                    max_tokens  = 2000,
                    temperature = 0.2,
                    messages    = new[] { new { role = "user", content = prompt } }
                };

                var llmResp = await llmHttp.PostAsync(
                    "https://api.groq.com/openai/v1/chat/completions",
                    new StringContent(JsonSerializer.Serialize(llmPayload), Encoding.UTF8, "application/json"));

                if (!llmResp.IsSuccessStatusCode)
                    return BuildFallbackOffers(nomProduit, quantite,
                        $"⚠️ Groq a renvoyé {llmResp.StatusCode} — offres exemples.");

                var llmJson = await llmResp.Content.ReadAsStringAsync();
                using var llmDoc = JsonDocument.Parse(llmJson);
                var raw = llmDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "[]";

                raw = Regex.Replace(raw, @"```json|```", "").Trim();

                // tolère un objet wrapper {"offres":[…]} ou un tableau direct
                if (raw.StartsWith("{"))
                {
                    var match = Regex.Match(raw, @"\[[\s\S]*\]");
                    if (match.Success) raw = match.Value;
                }

                var opts   = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var offres = JsonSerializer.Deserialize<List<OffreSearchResultDto>>(raw, opts)
                             ?? new List<OffreSearchResultDto>();

                // garantir un id stable + max 4
                foreach (var o in offres)
                    if (string.IsNullOrWhiteSpace(o.Id))
                        o.Id = Guid.NewGuid().ToString("N")[..8];

                if (offres.Count == 0)
                    return BuildFallbackOffers(nomProduit, quantite,
                        "⚠️ Le LLM n'a renvoyé aucune offre — offres exemples.");

                return offres.Take(4).ToList();
            }
            catch (Exception ex)
            {
                return BuildFallbackOffers(nomProduit, quantite,
                    $"⚠️ Erreur recherche : {ex.Message}");
            }
        }

        // ── Fallback offres (jamais silencieux : on l'indique dans la description) ──
        private static List<OffreSearchResultDto> BuildFallbackOffers(
            string nomProduit, int quantite, string raison)
        {
            var fournisseurs = new[] { "Fournisseur A", "Fournisseur B", "Fournisseur C", "Fournisseur D" };
            return fournisseurs.Select((f, i) => new OffreSearchResultDto
            {
                Id              = Guid.NewGuid().ToString("N")[..8],
                Fournisseur     = f,
                NomProduit      = nomProduit,
                Description     = raison,
                PrixUnitaire    = "N/A",
                PrixTotal       = "N/A",
                FraisLivraison  = "N/A",
                DelaiLivraison  = "N/A",
                Garantie        = "N/A",
                Devise          = "MAD",
                PointsForts     = new List<string> { "Offre exemple", "À configurer" }
            }).ToList();
        }

        // ── Résoudre une question vague grâce au contexte ────────────────────
        private async Task<string> ResolveQueryWithContextAsync(string query, List<AgentChatHistory>? history)
        {
            if (history == null || history.Count == 0) return query;

            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 5) return query;

            var groqKey = _config["GroqApiKey"];
            if (string.IsNullOrWhiteSpace(groqKey)) return query;

            try
            {
                var http = _factory.CreateClient();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {groqKey}");

                var historyText = string.Join("\n", history.TakeLast(6).Select(h =>
                    $"  [{(h.Role == "user" ? "Utilisateur" : "Assistant")}]: {(h.Content.Length > 200 ? h.Content[..197] + "..." : h.Content)}"));

                var prompt = $@"Voici une conversation. Le dernier message de l'utilisateur est peut-être vague ou fait référence à quelque chose mentionné avant.
Génère UNE SEULE requête de recherche web optimisée, autonome et explicite, qui capture l'intention réelle.

Conversation :
{historyText}

Dernier message : ""{query}""

Réponds UNIQUEMENT avec la requête de recherche (pas d'explication, pas de guillemets) :";

                var payload = new
                {
                    model      = "llama-3.3-70b-versatile",
                    max_tokens = 50,
                    messages   = new[] { new { role = "user", content = prompt } }
                };

                var resp = await http.PostAsync(
                    "https://api.groq.com/openai/v1/chat/completions",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                if (!resp.IsSuccessStatusCode) return query;

                var json = await resp.Content.ReadAsStringAsync();
                var doc  = JsonDocument.Parse(json);
                var resolved = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()?.Trim() ?? query;

                return string.IsNullOrWhiteSpace(resolved) ? query : resolved;
            }
            catch { return query; }
        }

        private static string BuildHistoryForLlm(List<AgentChatHistory>? history)
        {
            if (history == null || history.Count <= 1) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("Contexte de la conversation précédente :");
            foreach (var h in history.SkipLast(1).TakeLast(6))
            {
                var role    = h.Role == "user" ? "Utilisateur" : "Assistant";
                var content = h.Content.Length > 300 ? h.Content[..297] + "..." : h.Content;
                sb.AppendLine($"  [{role}]: {content}");
            }
            sb.AppendLine();
            return sb.ToString();
        }

        private async Task<string> CallGroqAsync(string key, string system, string user)
        {
            var http = _factory.CreateClient();
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);

            var body = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "system", content = system },
                    new { role = "user",   content = user   }
                },
                temperature = 0.3,
                max_tokens  = 1024
            };

            var resp = await http.PostAsJsonAsync("https://api.groq.com/openai/v1/chat/completions", body);
            if (!resp.IsSuccessStatusCode) return $"❌ Erreur Groq : {resp.StatusCode}";

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Pas de réponse.";
        }

        private async Task<string> CallMistralAsync(string key, string system, string user)
        {
            var http = _factory.CreateClient("MistralClient");
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);

            var body = new
            {
                model    = "mistral-small-latest",
                messages = new[]
                {
                    new { role = "system", content = system },
                    new { role = "user",   content = user   }
                },
                temperature = 0.3,
                max_tokens  = 1024
            };

            var resp = await http.PostAsJsonAsync("/v1/chat/completions", body);
            if (!resp.IsSuccessStatusCode) return $"❌ Erreur Mistral : {resp.StatusCode}";

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Pas de réponse.";
        }
    }
}
