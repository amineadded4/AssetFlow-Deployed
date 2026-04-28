namespace AssetFlow.Application.DTOs.AgentDtos
{
    public class AgentCommandeProposal
    {
        public string    NumeroCommande      { get; set; } = string.Empty;
        public int       MaterielId          { get; set; }
        public string    NomMateriel         { get; set; } = string.Empty;
        public int       FournisseurId       { get; set; }
        public string    NomFournisseur      { get; set; } = string.Empty;
        public int       QuantiteAchetee     { get; set; } = 1;
        public DateTime  DateAchat           { get; set; } = DateTime.UtcNow;
        public DateTime? DateLivraison       { get; set; }
        public DateTime? DateFinGarantie     { get; set; }
        public List<string?> NumerosSerie    { get; set; } = new();
    }
}