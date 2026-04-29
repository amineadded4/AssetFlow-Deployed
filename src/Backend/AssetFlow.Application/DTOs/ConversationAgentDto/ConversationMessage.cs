namespace AssetFlow.Application.DTOs
  {
      public class ConversationMessage
      {
          public string   Id        { get; set; } = Guid.NewGuid().ToString();
          public string   Role      { get; set; } = string.Empty; // "user" | "assistant"
          public string   Content   { get; set; } = string.Empty;
          public string?  AgentUsed { get; set; }
          public DateTime Timestamp { get; set; } = DateTime.UtcNow;

          /// <summary>Action IA sérialisée en JSON (si applicable)</summary>
          public string?  ActionJson      { get; set; }
          public bool     ActionProcessed { get; set; }

          /// <summary>
          /// NOUVEAU — Liste de groupes d'offres (un par matériel) sérialisée en JSON,
          /// pour ré-afficher les cartes lors du rechargement d'une conversation.
          /// </summary>
          public string?  OffersJson      { get; set; }
      }
  }
  