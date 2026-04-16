namespace AssetFlow.Application.DTOs
{
    public class MaterielStatsDto
    {
        public int TotalArticles   { get; set; }
        public int EnStock         { get; set; }
        public int AlerteSeuil     { get; set; }
        public int RuptureCritique { get; set; }
    }
}