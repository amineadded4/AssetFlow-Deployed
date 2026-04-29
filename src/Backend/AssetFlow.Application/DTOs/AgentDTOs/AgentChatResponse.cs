namespace AssetFlow.Application.DTOs.AgentDtos
{
     public class AgentChatResponse
    {
        public string AgentUsed        { get; set; } = string.Empty; // "web" | "db" | "orchestrator"
        public string Message          { get; set; } = string.Empty;
        public string? RawData         { get; set; }
        public AgentAction? Action     { get; set; } // si une action est proposée
        public List<AlerteStock> Alertes { get; set; } = new();

        public List<OffreSearchResultDto>? OffresWeb { get; set; }

        // ── NOUVEAU : contexte demande pour les Étapes 1 et 2 ─────────────
        public int?    IdDemande        { get; set; }
        public string? ReferenceDemande { get; set; }

        /// <summary>1 = recherche web, 2 = formulaire matériel pré-rempli.</summary>
        public int?    Etape            { get; set; }
    }
}