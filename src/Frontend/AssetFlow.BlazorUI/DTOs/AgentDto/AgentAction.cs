using System.Text.Json.Serialization;
namespace AssetFlow.BlazorUI.DTOs
{
    public class AgentAction
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("materielProposal")]
        public AgentMaterielProposal? MaterielProposal { get; set; }

        [JsonPropertyName("commandeProposal")]
        public AgentCommandeProposal? CommandeProposal { get; set; }

        [JsonPropertyName("articleProposal")]
        public AgentArticleProposal? ArticleProposal { get; set; }
    }
}