namespace AssetFlow.Application.DTOs
  {
      public class AddMessageRequest
      {
          public string  ConversationId  { get; set; } = string.Empty;
          public string  Role            { get; set; } = string.Empty;
          public string  Content         { get; set; } = string.Empty;
          public string? AgentUsed       { get; set; }
          public string? ActionJson      { get; set; }
          public bool    ActionProcessed { get; set; }

          /// <summary>NOUVEAU — JSON des offres (groupes par matériel).</summary>
          public string? OffersJson      { get; set; }
      }
  }
  