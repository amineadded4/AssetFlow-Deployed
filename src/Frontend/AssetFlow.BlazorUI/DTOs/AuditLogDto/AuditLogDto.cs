namespace AssetFlow.BlazorUI.DTOs
{
    public class AuditLogDto
    {
        public int      Id          { get; set; }
        public DateTime Timestamp   { get; set; }
        public string   Utilisateur { get; set; } = string.Empty;
        public string   Email       { get; set; } = string.Empty;
        public string   Action      { get; set; } = string.Empty;
        public string   Categorie   { get; set; } = string.Empty;
        public string   Entite      { get; set; } = string.Empty;
        public string?  Details     { get; set; }
        public string?  IpAddress   { get; set; }
        public string?  GeoLocation { get; set; }
    }
}