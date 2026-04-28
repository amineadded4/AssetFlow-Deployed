namespace AssetFlow.Application.DTOs
{
     public class CreateConversationResponse
    {
        public string   ConversationId { get; set; } = string.Empty;
        public string   Title          { get; set; } = string.Empty;
        public DateTime CreatedAt      { get; set; }
    }
}