namespace AssetFlow.Application.DTOs
{
    public class IncidentRawDto
    {
        public DateTime DateIncident    { get; set; }
        public DateTime? DateResolution { get; set; }
        public string   Statut          { get; set; } = string.Empty;
        public string   TypeIncident    { get; set; } = string.Empty;
        public int      Urgence         { get; set; }
        public int? MaterielId { get; set; }
    }
}