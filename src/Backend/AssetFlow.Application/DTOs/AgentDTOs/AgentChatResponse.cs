// src/Backend/AssetFlow.Application/DTOs/AgentDtos/AgentChatResponse.cs
  namespace AssetFlow.Application.DTOs.AgentDtos
  {
      public class AgentChatResponse
      {
          public string AgentUsed        { get; set; } = string.Empty; // "web" | "db" | "orchestrator"
          public string Message          { get; set; } = string.Empty;
          public string? RawData         { get; set; }
          public AgentAction? Action     { get; set; }
          public List<AlerteStock> Alertes { get; set; } = new();

          // ── Compat : offres "à plat" (1ʳᵉ ligne uniquement, pour rétro-compat) ──
          public List<OffreSearchResultDto>? OffresWeb { get; set; }

          // ── NOUVEAU : un groupe d'offres par matériel de la demande ─────────
          public List<MaterielOffersGroup>? OffersByMateriel { get; set; }

          // ── Contexte demande pour le workflow ──────────────────────────────
          public int?    IdDemande        { get; set; }
          public string? ReferenceDemande { get; set; }
          public int?    Etape            { get; set; }
      }
  }
  