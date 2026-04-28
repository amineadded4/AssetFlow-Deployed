namespace AssetFlow.Application.DTOs.AgentDtos
{
    public class AgentChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<AgentChatHistory> History { get; set; } = new();
    }
}