namespace AssetFlow.BlazorUI.DTOs
{
    public class ArticleResumDto
    {
        public int ArticleId { get; set; }
        public string NumeroSerie { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public string Etat { get; set; } = string.Empty;
        public string? AffecteA { get; set; }
    }
}