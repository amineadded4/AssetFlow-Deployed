namespace AssetFlow.Application.DTOs
{
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
}