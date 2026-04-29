namespace AssetFlow.Application.DTOs.AgentDtos
{
    public class AgentApprovalRequest
    {
        public string ActionType   { get; set; } = string.Empty;
        public bool   Approved     { get; set; }
        public string Utilisateur  { get; set; } = string.Empty;
        
        public AgentMaterielProposal? MaterielProposal   { get; set; }
        public AgentCommandeProposal? CommandeProposal   { get; set; }
        public AgentArticleProposal?  ArticleProposal    { get; set; }
        public int? IdDemandeOrigine { get; set; }
    }
}