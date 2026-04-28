using System.Text.Json.Serialization;
namespace AssetFlow.BlazorUI.DTOs
{
    public class AgentApprovalResponse
    {
        [JsonPropertyName("succes")]
        public bool Succes { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public int? Id { get; set; }
    }
}