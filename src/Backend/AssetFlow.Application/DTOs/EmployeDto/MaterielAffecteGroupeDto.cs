namespace AssetFlow.Application.DTOs
{
    public class MaterielAffecteGroupeDto
    {
        public int      MaterielId          { get; set; }
        public string   Reference           { get; set; } = string.Empty;
        public string   Designation         { get; set; } = string.Empty;
        public string   Categorie           { get; set; } = string.Empty;
        public string?  ImageUrl            { get; set; }
        public int      NombreArticles      { get; set; }
        public string   StatutDominant      { get; set; } = string.Empty;
        public string   StatutBadgeColor    { get; set; } = string.Empty;
        public DateTime DerniereAffectation { get; set; }
        public int      NombreCommentaires  { get; set; } = 0;   // ← NOUVEAU
        public List<ArticleAffecteDto> Articles { get; set; } = new();
    }
}