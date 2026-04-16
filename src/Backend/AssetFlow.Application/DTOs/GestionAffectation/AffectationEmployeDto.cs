namespace AssetFlow.Application.DTOs
{
    public class AffectationEmployeDto
    {
        public int      AffectationId    { get; set; }
        public int      MaterielId       { get; set; }
        public string   Designation      { get; set; } = string.Empty;
        public string   Reference        { get; set; } = string.Empty;
        public string   Categorie        { get; set; } = string.Empty;
        public string?  ImageUrl         { get; set; }
        public DateTime DateAffectation  { get; set; }
        public DateTime? DateRetourPrevue { get; set; }
        public string   Etat             { get; set; } = string.Empty;
        public string?  Observations     { get; set; }
        public List<ArticleAffectationDto> Articles { get; set; } = new();
    }
}