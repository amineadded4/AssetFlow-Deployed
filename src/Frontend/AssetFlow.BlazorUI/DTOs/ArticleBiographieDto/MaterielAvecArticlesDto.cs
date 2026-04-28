namespace AssetFlow.BlazorUI.DTOs
{
     public class MaterielAvecArticlesDto
    {
        public int MaterielId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Categorie { get; set; } = string.Empty;
        public List<ArticleResumDto> Articles { get; set; } = new();
    }
}