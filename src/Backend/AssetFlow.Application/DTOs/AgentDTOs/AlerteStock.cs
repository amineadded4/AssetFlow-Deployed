namespace AssetFlow.Application.DTOs.AgentDtos
{
    public class AlerteStock
    {
        public int    MaterielId   { get; set; }
        public string Reference    { get; set; } = string.Empty;
        public string Designation  { get; set; } = string.Empty;
        public int    QuantiteStock { get; set; }
        public int    QuantiteMin  { get; set; }
        public string Categorie    { get; set; } = string.Empty;
        public AgentMaterielProposal? Proposition { get; set; }
    }
}