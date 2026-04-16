namespace AssetFlow.Application.DTOs
{
    public class SignalerIncidentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IncidentId { get; set; }
        public string? NumeroIncident { get; set; }
    }
}