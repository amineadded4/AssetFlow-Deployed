namespace AssetFlow.BlazorUI.DTOs
{
    public class ConversationCreatedDto
        {
            public string   ConversationId { get; set; } = string.Empty;
            public string   Title          { get; set; } = string.Empty;
            public DateTime CreatedAt      { get; set; }
        }
}