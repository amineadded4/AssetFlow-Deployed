namespace AssetFlow.Application.DTOs
{
    public class AuditLogPagedDto
    {
        public List<AuditLogDto> Items      { get; set; } = new();
        public int               Total      { get; set; }
        public int               Page       { get; set; }
        public int               PageSize   { get; set; }
        public int               TotalPages { get; set; }
    }
}