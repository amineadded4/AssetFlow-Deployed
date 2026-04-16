namespace AssetFlow.Application.DTOs
{
    public class ArticleDto
    {
        public int Id { get; set; }
        public string? NumeroSerie { get; set; }
        public string Statut { get; set; } = string.Empty;
        public int CommandeId { get; set; }
        public string NumeroCommande { get; set; } = string.Empty;
    }
}