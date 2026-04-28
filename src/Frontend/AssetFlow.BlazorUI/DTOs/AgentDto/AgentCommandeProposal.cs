using System.Text.Json.Serialization;
namespace AssetFlow.BlazorUI.DTOs
{
    public class AgentCommandeProposal
    {
        [JsonPropertyName("numeroCommande")]
        public string NumeroCommande { get; set; } = string.Empty;

        [JsonPropertyName("materielId")]
        public int MaterielId { get; set; }

        [JsonPropertyName("nomMateriel")]
        public string NomMateriel { get; set; } = string.Empty;

        [JsonPropertyName("fournisseurId")]
        public int FournisseurId { get; set; }

        [JsonPropertyName("nomFournisseur")]
        public string NomFournisseur { get; set; } = string.Empty;

        [JsonPropertyName("quantiteAchetee")]
        public int QuantiteAchetee { get; set; } = 1;

        [JsonPropertyName("dateAchat")]
        public DateTime DateAchat { get; set; } = DateTime.Today;

        [JsonPropertyName("dateLivraison")]
        public DateTime? DateLivraison { get; set; }

        [JsonPropertyName("dateFinGarantie")]
        public DateTime? DateFinGarantie { get; set; }

        [JsonPropertyName("numerosSerie")]
        public List<string?> NumerosSerie { get; set; } = new();
    }
}