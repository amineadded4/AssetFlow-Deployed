namespace AssetFlow.Application.DTOs.AgentDtos
{
    public class AgentAction
    {
        public string Type { get; set; } = string.Empty; // "add_materiel" | "add_commande" | "add_article"
        public string Label { get; set; } = string.Empty;
        
        // Formulaire pré-rempli pour approbation
        public AgentMaterielProposal? MaterielProposal   { get; set; }
        public AgentCommandeProposal? CommandeProposal   { get; set; }
        public AgentArticleProposal?  ArticleProposal    { get; set; }
    }
}