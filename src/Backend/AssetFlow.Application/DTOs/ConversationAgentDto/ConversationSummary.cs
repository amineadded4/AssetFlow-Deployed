namespace AssetFlow.Application.DTOs
{
    public class ConversationSummary
    {
        public string   Id           { get; set; } = string.Empty;
        public int      UserId       { get; set; }
        public string   Title        { get; set; } = "Nouvelle conversation";
        public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt    { get; set; } = DateTime.UtcNow;
        public int      MessageCount { get; set; }
        public string?  LastMessage  { get; set; }
    }
}