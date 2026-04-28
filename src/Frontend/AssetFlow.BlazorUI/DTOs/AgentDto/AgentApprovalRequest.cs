using System.Text.Json.Serialization;
namespace AssetFlow.BlazorUI.DTOs
{
    public class AgentApprovalRequest
    {
        [JsonPropertyName("actionType")]
        public string ActionType { get; set; } = string.Empty;

        [JsonPropertyName("approved")]
        public bool Approved { get; set; }

        [JsonPropertyName("utilisateur")]
        public string Utilisateur { get; set; } = string.Empty;

        [JsonPropertyName("materielProposal")]
        public AgentMaterielProposal? MaterielProposal { get; set; }

        [JsonPropertyName("commandeProposal")]
        public AgentCommandeProposal? CommandeProposal { get; set; }

        [JsonPropertyName("articleProposal")]
        public AgentArticleProposal? ArticleProposal { get; set; }
    }
}