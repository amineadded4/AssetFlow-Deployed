namespace AssetFlow.Application.DTOs
{
    /// <summary>Vue hiérarchique : Employé → Matériels → Articles → Incidents</summary>
    public class IncidentArticleDto
    {
        public int    ArticleId    { get; set; }
        public string NumeroSerie  { get; set; } = string.Empty;
        public string EtatArticle  { get; set; } = string.Empty;
        public List<IncidentDto> Incidents { get; set; } = new();
    }
}