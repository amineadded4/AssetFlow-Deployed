namespace AssetFlow.Application.DTOs
{
    public class MaterielDisponibleDto
    {
        public int    Id                 { get; set; }
        public string Reference          { get; set; } = string.Empty;
        public string Designation        { get; set; } = string.Empty;
        public string Categorie          { get; set; } = string.Empty;
        public string? ImageUrl          { get; set; }
        public int    QuantiteDisponible { get; set; }
        public List<ArticleDisponibleDto> Articles { get; set; } = new();
    }
}