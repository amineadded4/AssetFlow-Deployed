namespace AssetFlow.Application.DTOs
{
    public class SignalerIncidentRequestDto
    {
        public int AffectationId { get; set; }
        public int? ArticleId { get; set; }  // ← AJOUTER
        public string TypeIncident { get; set; } = string.Empty;
        public int Urgence { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}