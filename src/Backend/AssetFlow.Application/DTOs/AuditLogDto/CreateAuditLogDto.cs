namespace AssetFlow.Application.DTOs
{
     public class CreateAuditLogDto
    {
        public string   Utilisateur { get; set; } = string.Empty;
        public string   Email       { get; set; } = string.Empty;
        public string   Action      { get; set; } = string.Empty;
        public string   Categorie   { get; set; } = string.Empty;
        public string   Entite      { get; set; } = string.Empty;
        public string?  Details     { get; set; }
        public int?     UserId      { get; set; }
        public string? IpAddress    { get; set; }
        public string? GeoLocation  { get; set; }
    }
}
