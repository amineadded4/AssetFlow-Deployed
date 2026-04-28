namespace AssetFlow.Application.DTOs.AgentDtos
{
    public class AgentMaterielProposal
    {
        public string  Reference     { get; set; } = string.Empty;
        public string  Designation   { get; set; } = string.Empty;
        public string? Description   { get; set; }
        public string  Categorie     { get; set; } = string.Empty;
        public int     QuantiteStock { get; set; }
        public int     QuantiteMin   { get; set; }
        public string  Unite         { get; set; } = "pièce";
        public string? Emplacement   { get; set; }
        
        // Commande associée (optionnel)
        public AgentCommandeProposal? Commande { get; set; }
    }
}