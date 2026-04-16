namespace AssetFlow.Application.DTOs
{
    public class EquipementAffecteDto
    {
        public int      AffectationId    { get; set; }
        public int      MaterielId       { get; set; }
        public string   Reference        { get; set; } = string.Empty;
        public string   Designation      { get; set; } = string.Empty;
        public string   Categorie        { get; set; } = string.Empty;
        public string?  ImageUrl         { get; set; }
        public DateTime DateAffectation  { get; set; }
        public int      QuantiteAffectee { get; set; }
        public string   StatutBadgeColor { get; set; } = string.Empty;
        public string?  Observations     { get; set; }
        public string?  NumeroSerie      { get; set; }
        public string   EtatArticle      { get; set; } = "Bon";
    }
}