using System.Text.Json.Serialization;
namespace AssetFlow.BlazorUI.DTOs
{
    public class AgentArticleProposal
    {
        [JsonPropertyName("materielId")]
        public int MaterielId { get; set; }

        [JsonPropertyName("nomMateriel")]
        public string NomMateriel { get; set; } = string.Empty;

        [JsonPropertyName("commandeId")]
        public int CommandeId { get; set; }

        [JsonPropertyName("numeroSerie")]
        public string? NumeroSerie { get; set; }
    }
}