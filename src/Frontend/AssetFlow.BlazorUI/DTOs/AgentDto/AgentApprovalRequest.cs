// src/Frontend/AssetFlow.BlazorUI/DTOs/AgentApprovalRequest.cs
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

        // ── NOUVEAU : si l'approbation provient d'une demande d'achat,
        //              on marque la demande comme "commande" après création.
        [JsonPropertyName("idDemandeOrigine")]
        public int? IdDemandeOrigine { get; set; }
    }
}
