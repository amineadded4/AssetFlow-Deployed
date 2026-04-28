namespace AssetFlow.Application.DTOs.AgentDtos
{
    public class AgentArticleProposal
    {
        public int     MaterielId  { get; set; }
        public string  NomMateriel { get; set; } = string.Empty;
        public int     CommandeId  { get; set; }
        public string? NumeroSerie { get; set; }
    }
}