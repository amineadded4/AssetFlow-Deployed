namespace AssetFlow.Application.DTOs
{
    public class ChangerStatutIncidentDto
    {
        public string NouveauStatut          { get; set; } = string.Empty; // "EnCours" | "Resolu"
        public string? CommentairesResolution { get; set; }
    }
}