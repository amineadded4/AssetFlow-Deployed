namespace AssetFlow.Application.DTOs
{
    public class IncidentMaterielDto
    {
        public int    MaterielId  { get; set; }
        public string Designation { get; set; } = string.Empty;
        public string Reference   { get; set; } = string.Empty;
        public string? ImageUrl   { get; set; }
        public string Categorie   { get; set; } = string.Empty;
        public int    AffectationId { get; set; }
        public int    NbIncidentsActifs { get; set; }
        public List<IncidentArticleDto> Articles { get; set; } = new();
    }
}