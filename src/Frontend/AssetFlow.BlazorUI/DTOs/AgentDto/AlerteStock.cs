using System.Text.Json.Serialization;
namespace AssetFlow.BlazorUI.DTOs
{
    public class AlerteStock
    {
        [JsonPropertyName("materielId")]
        public int MaterielId { get; set; }

        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;

        [JsonPropertyName("designation")]
        public string Designation { get; set; } = string.Empty;

        [JsonPropertyName("quantiteStock")]
        public int QuantiteStock { get; set; }

        [JsonPropertyName("quantiteMin")]
        public int QuantiteMin { get; set; }

        [JsonPropertyName("categorie")]
        public string Categorie { get; set; } = string.Empty;

        [JsonPropertyName("proposition")]
        public AgentMaterielProposal? Proposition { get; set; }
    }
}