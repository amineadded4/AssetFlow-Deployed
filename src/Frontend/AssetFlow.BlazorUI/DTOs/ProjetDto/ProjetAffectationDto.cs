namespace AssetFlow.BlazorUI.DTOs
{
    public class ProjetAffectationDto
    {
        public int       AffectationId    { get; set; }
        public string    Designation      { get; set; } = string.Empty;
        public string    Reference        { get; set; } = string.Empty;
        public int       QuantiteAffectee { get; set; }
        public DateTime  DateAffectation  { get; set; }
        public DateTime? DateRetourPrevue { get; set; }
        public string    Etat             { get; set; } = string.Empty;
        public string?  ImageUrl         { get; set; }
    }
}