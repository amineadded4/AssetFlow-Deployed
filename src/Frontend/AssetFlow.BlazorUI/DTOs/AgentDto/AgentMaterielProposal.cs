using System.Text.Json.Serialization;
namespace AssetFlow.BlazorUI.DTOs
{
    public class AgentMaterielProposal
    {
        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;

        [JsonPropertyName("designation")]
        public string Designation { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("categorie")]
        public string Categorie { get; set; } = string.Empty;

        [JsonPropertyName("quantiteStock")]
        public int QuantiteStock { get; set; }

        [JsonPropertyName("quantiteMin")]
        public int QuantiteMin { get; set; }

        [JsonPropertyName("unite")]
        public string Unite { get; set; } = "pièce";

        [JsonPropertyName("emplacement")]
        public string? Emplacement { get; set; }

        [JsonPropertyName("commande")]
        public AgentCommandeProposal? Commande { get; set; }
    }
}