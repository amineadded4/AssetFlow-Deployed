// src/Frontend/AssetFlow.BlazorUI/DTOs/AgentChatResponse.cs
using System.Text.Json.Serialization;

namespace AssetFlow.BlazorUI.DTOs
{
    public class AgentChatResponse
    {
        [JsonPropertyName("agentUsed")]
        public string AgentUsed { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public AgentAction? Action { get; set; }

        [JsonPropertyName("alertes")]
        public List<AlerteStock> Alertes { get; set; } = new();

        // ── NOUVEAU : workflow Demande d'achat ─────────────────────────────
        [JsonPropertyName("offresWeb")]
        public List<OffreSearchResultDto>? OffresWeb { get; set; }

        [JsonPropertyName("idDemande")]
        public int? IdDemande { get; set; }

        [JsonPropertyName("referenceDemande")]
        public string? ReferenceDemande { get; set; }

        /// <summary>1 = recherche web (5 offres), 2 = formulaire matériel pré-rempli.</summary>
        [JsonPropertyName("etape")]
        public int? Etape { get; set; }
    }
}
