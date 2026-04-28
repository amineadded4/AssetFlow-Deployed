namespace AssetFlow.BlazorUI.DTOs
{
    public class ConversationSummary
        {
            public string   Id           { get; set; } = string.Empty;
            public string   Title        { get; set; } = string.Empty;
            public DateTime CreatedAt    { get; set; }
            public DateTime UpdatedAt    { get; set; }
            public int      MessageCount { get; set; }
            public string?  LastMessage  { get; set; }
        }
}