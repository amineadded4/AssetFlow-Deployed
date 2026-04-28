namespace AssetFlow.Application.DTOs
{
    public class ConversationMessageDto
    {
        public string   Id             { get; set; } = string.Empty;
        public string   Role           { get; set; } = string.Empty;
        public string   Content        { get; set; } = string.Empty;
        public string?  AgentUsed      { get; set; }
        public DateTime Timestamp      { get; set; }
        public string?  ActionJson     { get; set; }
        public bool     ActionProcessed { get; set; }
    }
}