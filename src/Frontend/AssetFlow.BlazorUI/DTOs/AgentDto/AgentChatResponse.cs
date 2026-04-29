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

          // ── Compat : offres "à plat" (1ʳᵉ ligne uniquement) ────────────────
          [JsonPropertyName("offresWeb")]
          public List<OffreSearchResultDto>? OffresWeb { get; set; }

          // ── NOUVEAU : un groupe d'offres par matériel de la demande ─────────
          [JsonPropertyName("offersByMateriel")]
          public List<MaterielOffersGroupDto>? OffersByMateriel { get; set; }

          // ── Contexte demande pour le workflow ──────────────────────────────
          [JsonPropertyName("idDemande")]
          public int? IdDemande { get; set; }

          [JsonPropertyName("referenceDemande")]
          public string? ReferenceDemande { get; set; }

          [JsonPropertyName("etape")]
          public int? Etape { get; set; }
      }
  }
  