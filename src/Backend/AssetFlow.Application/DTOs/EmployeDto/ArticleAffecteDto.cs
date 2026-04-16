namespace AssetFlow.Application.DTOs
{
    public class ArticleAffecteDto
    {
        public int      AffectationId    { get; set; }
        public int      ArticleId        { get; set; }
        public string   NumeroSerie      { get; set; } = string.Empty;
        public string   StatutArticle    { get; set; } = string.Empty;
        public string   EtatArticle      { get; set; } = "Bon";
        public string   StatutBadgeColor { get; set; } = string.Empty;
        public DateTime DateAffectation  { get; set; }
        public string?  Observations     { get; set; }
    }
}