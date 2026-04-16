namespace AssetFlow.Application.DTOs
{
    public class ChangerStatutDto
    {
        public string  Statut     { get; set; } = string.Empty;
        public string? MotifRefus { get; set; }
        public string Utilisateur { get; set; } = string.Empty;
    }
}