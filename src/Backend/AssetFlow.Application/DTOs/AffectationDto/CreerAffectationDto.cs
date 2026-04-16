namespace AssetFlow.Application.DTOs
{
    public class CreerAffectationDto
    {
        public string    user_name        { get; set; } = string.Empty;
        public int       MaterielId       { get; set; }
        public int?       UtilisateurId    { get; set; }
        public List<int> ArticleIds       { get; set; } = new();
        public string?   Observations     { get; set; }
        public DateTime? DateRetourPrevue { get; set; }
        public int? ProjetId { get; set; }
    }

}