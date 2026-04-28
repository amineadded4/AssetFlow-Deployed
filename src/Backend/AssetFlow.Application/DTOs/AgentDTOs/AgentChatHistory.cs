namespace AssetFlow.Application.DTOs.AgentDtos
{
    public class AgentChatHistory
    {
        public string Role    { get; set; } = string.Empty; // "user" | "assistant"
        public string Content { get; set; } = string.Empty;
    }
}