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
    }

    public class AuditLogPagedDto
    {
        public List<AuditLogDto> Items      { get; set; } = new();
        public int               Total      { get; set; }
        public int               Page       { get; set; }
        public int               PageSize   { get; set; }
        public int               TotalPages { get; set; }
    }
}