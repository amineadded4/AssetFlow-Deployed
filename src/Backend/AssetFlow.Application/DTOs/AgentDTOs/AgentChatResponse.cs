namespace AssetFlow.Application.DTOs.AgentDtos
{
     public class AgentChatResponse
    {
        public string AgentUsed        { get; set; } = string.Empty; // "web" | "db" | "orchestrator"
        public string Message          { get; set; } = string.Empty;
        public string? RawData         { get; set; }
        public AgentAction? Action     { get; set; } // si une action est proposée
        public List<AlerteStock> Alertes { get; set; } = new();
    }
}