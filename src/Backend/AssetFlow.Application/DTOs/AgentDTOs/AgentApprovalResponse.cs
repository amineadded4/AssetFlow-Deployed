namespace AssetFlow.Application.DTOs.AgentDtos
{
    public class AgentApprovalResponse
    {
        public bool   Succes  { get; set; }
        public string Message { get; set; } = string.Empty;
        public int?   Id      { get; set; }
    }
}