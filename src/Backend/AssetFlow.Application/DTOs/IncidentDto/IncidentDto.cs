namespace AssetFlow.Application.DTOs
{
    public class IncidentDto
    {
        public int Id { get; set; }
        public int AffectationId { get; set; }
        public string NumeroIncident { get; set; } = string.Empty;
        public string TypeIncident { get; set; } = string.Empty;
        public int Urgence { get; set; }
        public string UrgenceLabel { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DateIncident { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string StatutLabel { get; set; } = string.Empty;
        public DateTime? DateResolution { get; set; }
        public string? CommentairesResolution { get; set; }
        
        // Infos matériel
        public string MaterielDesignation { get; set; } = string.Empty;
        public string MaterielReference { get; set; } = string.Empty;
    }
}