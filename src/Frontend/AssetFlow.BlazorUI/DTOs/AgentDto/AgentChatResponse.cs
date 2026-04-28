using System.Text.Json.Serialization;
namespace AssetFlow.BlazorUI.DTOs
{
    public class AgentChatResponse
    {
        [JsonPropertyName("agentUsed")]
        public string AgentUsed { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public AgentAction? Action { get; set; }

        [JsonPropertyName("alertes")]
        public List<AlerteStock> Alertes { get; set; } = new();
    }
}