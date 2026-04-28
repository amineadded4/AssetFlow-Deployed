// src/Backend/AssetFlow.Infrastructure/Services/OrchestratorAgentService.cs
using AssetFlow.Application.DTOs.AgentDtos;
using AssetFlow.Application.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace AssetFlow.Infrastructure.Services
{
    public class OrchestratorAgentService : IOrchestratorAgentService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration    _config;

        public OrchestratorAgentService(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory = httpFactory;
            _config      = config;
        }

        // ── Détermine quel agent appeler ─────────────────────────────────
        public async Task<string> DetermineAgentAsync(string userMessage)
        {
            var groqKey = _config["GroqApiKey"];
            if (string.IsNullOrWhiteSpace(groqKey)) return "db";

            var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {groqKey}");

            var prompt = $@"Tu es un orchestrateur d'agents pour un système de gestion de stock/actifs IT.

Analyse le message utilisateur et décide quel agent utiliser. 
Réponds UNIQUEMENT avec un de ces mots exactement: web, db, action_add_materiel, action_add_commande, action_add_article

Règles:
- 'web' : recherche internet, tendances marché, infos fournisseurs externes, comparaisons prix
- 'db' : questions sur les données de la base (liste matériels, stock, commandes, incidents, stats, affectations, qui a quoi, combien...)
- 'action_add_materiel' : l'utilisateur veut ajouter/créer un nouveau matériel
- 'action_add_commande' : l'utilisateur veut créer/passer une nouvelle commande
- 'action_add_article' : l'utilisateur veut ajouter un article individuel à une commande

Exemples:
- 'liste mes matériels' → db
- 'combien de PC en stock' → db
- 'quel est le prix des SSD sur le marché' → web
- 'trouve moi des fournisseurs de claviers' → web
- 'ajoute un nouveau matériel laptop Dell' → action_add_materiel
- 'je veux créer une commande pour les écrans' → action_add_commande
- 'ajoute un article à la commande CMD-001' → action_add_article
- 'quelles sont mes alertes de stock' → db

Message: ""{userMessage}""

Réponse (un seul mot):";

            var payload = new
            {
                model      = "llama-3.3-70b-versatile",
                max_tokens = 10,
                messages   = new[] { new { role = "user", content = prompt } }
            };

            var resp = await http.PostAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            if (!resp.IsSuccessStatusCode) return "db";

            var json   = await resp.Content.ReadAsStringAsync();
            var doc    = JsonDocument.Parse(json);
            var result = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim().ToLower() ?? "db";

            var valid = new[] { "web", "db", "action_add_materiel", "action_add_commande", "action_add_article" };
            return valid.Contains(result) ? result : "db";
        }

        // ── Extrait une action structurée du message ──────────────────────
        public async Task<AgentAction?> ExtractActionAsync(string userMessage)
        {
            var groqKey = _config["GroqApiKey"];
            if (string.IsNullOrWhiteSpace(groqKey)) return null;

            var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {groqKey}");

            // Déterminer le type d'action
            var agentType = await DetermineAgentAsync(userMessage);
            if (!agentType.StartsWith("action_")) return null;

            var actionType = agentType.Replace("action_", "add_");

            var prompt = agentType switch
            {
                "action_add_materiel" => $@"L'utilisateur veut ajouter un matériel. Extrait les infos du message et génère un JSON.
Message: ""{userMessage}""

Réponds UNIQUEMENT avec ce JSON (sans markdown):
{{
  ""reference"": ""REF-XXX"",
  ""designation"": ""Nom du matériel"",
  ""description"": ""Description optionnelle"",
  ""categorie"": ""Infrastructure"",
  ""quantiteStock"": 0,
  ""quantiteMin"": 5,
  ""unite"": ""pièce"",
  ""emplacement"": null
}}
Si une info manque, mets une valeur par défaut raisonnable. Catégorie doit être 'Infrastructure' ou 'Normal'.",

                "action_add_commande" => $@"L'utilisateur veut créer une commande. Extrait les infos du message et génère un JSON.
Message: ""{userMessage}""

Réponds UNIQUEMENT avec ce JSON (sans markdown):
{{
  ""numeroCommande"": ""CMD-2026-XXX"",
  ""nomMateriel"": ""Nom du matériel concerné"",
  ""materielId"": 0,
  ""nomFournisseur"": ""Nom fournisseur si mentionné"",
  ""fournisseurId"": 0,
  ""quantiteAchetee"": 1,
  ""dateAchat"": ""{DateTime.UtcNow:yyyy-MM-dd}"",
  ""dateLivraison"": null,
  ""dateFinGarantie"": null
}}",

                "action_add_article" => $@"L'utilisateur veut ajouter un article. Extrait les infos du message et génère un JSON.
Message: ""{userMessage}""

Réponds UNIQUEMENT avec ce JSON (sans markdown):
{{
  ""materielId"": 0,
  ""nomMateriel"": ""Nom du matériel"",
  ""commandeId"": 0,
  ""numeroSerie"": null
}}",
                _ => null
            };

            if (prompt == null) return null;

            var payload = new
            {
                model      = "llama-3.3-70b-versatile",
                max_tokens = 400,
                messages   = new[] { new { role = "user", content = prompt } }
            };

            var resp = await http.PostAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            if (!resp.IsSuccessStatusCode) return null;

            var json     = await resp.Content.ReadAsStringAsync();
            var doc      = JsonDocument.Parse(json);
            var rawJson  = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "{}";

            // Nettoyer le JSON
            rawJson = Regex.Replace(rawJson, @"```json|```", "").Trim();

            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var action = new AgentAction { Type = actionType };

                if (agentType == "action_add_materiel")
                {
                    action.Label           = "Nouveau matériel à créer";
                    action.MaterielProposal = JsonSerializer.Deserialize<AgentMaterielProposal>(rawJson, opts);
                }
                else if (agentType == "action_add_commande")
                {
                    action.Label            = "Nouvelle commande à créer";
                    action.CommandeProposal = JsonSerializer.Deserialize<AgentCommandeProposal>(rawJson, opts);
                }
                else if (agentType == "action_add_article")
                {
                    action.Label           = "Nouvel article à ajouter";
                    action.ArticleProposal = JsonSerializer.Deserialize<AgentArticleProposal>(rawJson, opts);
                }

                return action;
            }
            catch { return null; }
        }

        // ── Génère une proposition de matériel pour une alerte stock ───────
        public async Task<AgentMaterielProposal> GenerateMaterielProposalAsync(AlerteStock alerte)
        {
            var groqKey = _config["GroqApiKey"];
            
            // Proposition par défaut
            var defaultProposal = new AgentMaterielProposal
            {
                Reference     = $"REAPRO-{alerte.Reference}",
                Designation   = $"{alerte.Designation} (réapprovisionnement)",
                Categorie     = alerte.Categorie,
                QuantiteStock = alerte.QuantiteMin * 3,
                QuantiteMin   = alerte.QuantiteMin,
                Unite         = "pièce",
                Description   = $"Réapprovisionnement automatique pour {alerte.Designation}",
                Commande = new AgentCommandeProposal
                {
                    NumeroCommande  = $"CMD-{DateTime.UtcNow:yyyy}-REAPRO-{alerte.Reference}-{DateTime.UtcNow:MMdd}",
                    MaterielId      = alerte.MaterielId,
                    NomMateriel     = alerte.Designation,
                    QuantiteAchetee = alerte.QuantiteMin * 3,
                    DateAchat       = DateTime.UtcNow
                }
            };

            if (string.IsNullOrWhiteSpace(groqKey)) return defaultProposal;

            try
            {
                var http = _httpFactory.CreateClient();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {groqKey}");

                var prompt = $@"Un matériel est en alerte de stock dans notre système de gestion d'actifs.
Génère une proposition de commande de réapprovisionnement en JSON.

Matériel: {alerte.Designation} (ref: {alerte.Reference})
Stock actuel: {alerte.QuantiteStock} / Minimum requis: {alerte.QuantiteMin}
Catégorie: {alerte.Categorie}

Réponds UNIQUEMENT avec ce JSON (sans markdown):
{{
  ""reference"": ""{alerte.Reference}"",
  ""designation"": ""{alerte.Designation}"",
  ""description"": ""Description courte"",
  ""categorie"": ""{alerte.Categorie}"",
  ""quantiteStock"": {alerte.QuantiteMin * 3},
  ""quantiteMin"": {alerte.QuantiteMin},
  ""unite"": ""pièce"",
  ""commande"": {{
    ""numeroCommande"": ""CMD-{DateTime.UtcNow:yyyy}-REAPRO-{alerte.Reference}-{DateTime.UtcNow:MMdd}"",
    ""materielId"": {alerte.MaterielId},
    ""nomMateriel"": ""{alerte.Designation}"",
    ""nomFournisseur"": """",
    ""fournisseurId"": 0,
    ""quantiteAchetee"": {alerte.QuantiteMin * 3},
    ""dateAchat"": ""{DateTime.UtcNow:yyyy-MM-dd}"",
    ""dateLivraison"": ""{DateTime.UtcNow.AddDays(14):yyyy-MM-dd}"",
    ""dateFinGarantie"": null
  }}
}}";

                var payload = new
                {
                    model      = "llama-3.3-70b-versatile",
                    max_tokens = 400,
                    messages   = new[] { new { role = "user", content = prompt } }
                };

                var resp = await http.PostAsync(
                    "https://api.groq.com/openai/v1/chat/completions",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                if (!resp.IsSuccessStatusCode) return defaultProposal;

                var json    = await resp.Content.ReadAsStringAsync();
                var doc     = JsonDocument.Parse(json);
                var rawJson = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "{}";

                rawJson = Regex.Replace(rawJson, @"```json|```", "").Trim();
                var opts   = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AgentMaterielProposal>(rawJson, opts);
                return result ?? defaultProposal;
            }
            catch { return defaultProposal; }
        }
    }
}
