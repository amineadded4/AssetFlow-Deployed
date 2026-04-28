namespace AssetFlow.BlazorUI.DTOs
{
    public class EvenementArticleDto
    {
        public int Id { get; set; }
        public string TypeEvenement { get; set; } = string.Empty;
        public DateTime DateEvenement { get; set; }
        public string? UtilisateurNom { get; set; }
        public string? Description { get; set; }
        public int? DureeDepuisPrecedent { get; set; }
    }
}