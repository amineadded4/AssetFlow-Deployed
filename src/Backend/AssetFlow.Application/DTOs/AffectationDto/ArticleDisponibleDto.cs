namespace AssetFlow.Application.DTOs
{
    public class ArticleDisponibleDto
    {
        public int    Id          { get; set; }
        public string NumeroSerie { get; set; } = string.Empty;
        public string Etat        { get; set; } = "Bon";
    }
}