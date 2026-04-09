namespace AssetFlow.Application.DTOs
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

    public class AuditLogQueryDto
    {
        public DateTime? DateDebut   { get; set; }
        public DateTime? DateFin     { get; set; }
        public string?   Utilisateur { get; set; }
        public string?   Action      { get; set; }
        public string?   Categorie   { get; set; }
        public string?   Search      { get; set; }
        public int       Page        { get; set; } = 1;
        public int       PageSize    { get; set; } = 50;
    }

    public class AuditLogPagedDto
    {
        public List<AuditLogDto> Items      { get; set; } = new();
        public int               Total      { get; set; }
        public int               Page       { get; set; }
        public int               PageSize   { get; set; }
        public int               TotalPages { get; set; }
    }

    /// <summary>Utilisé en interne pour enregistrer une action</summary>
    public class CreateAuditLogDto
    {
        public string   Utilisateur { get; set; } = string.Empty;
        public string   Email       { get; set; } = string.Empty;
        public string   Action      { get; set; } = string.Empty;
        public string   Categorie   { get; set; } = string.Empty;
        public string   Entite      { get; set; } = string.Empty;
        public string?  Details     { get; set; }
        public int?     UserId      { get; set; }
    }
}