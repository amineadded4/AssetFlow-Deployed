namespace AssetFlow.BlazorUI.DTOs
{
     public class ProjectDto
    {
        public int       Id          { get; set; }
        public string    Nom         { get; set; } = string.Empty;
        public string?   Description { get; set; }
        public string    Statut      { get; set; } = "Planifie";
        public string    Priorite    { get; set; } = "Moyenne";
        public string?   Responsable { get; set; }
        public decimal?  Budget      { get; set; }
        public DateTime? DateDebut   { get; set; }
        public DateTime? DateFin     { get; set; }
        public DateTime  CreatedAt   { get; set; }
        public DateTime  UpdatedAt   { get; set; }
    }

    public class ProjetAffectationDto
    {
        public int       AffectationId    { get; set; }
        public string    Designation      { get; set; } = string.Empty;
        public string    Reference        { get; set; } = string.Empty;
        public int       QuantiteAffectee { get; set; }
        public DateTime  DateAffectation  { get; set; }
        public DateTime? DateRetourPrevue { get; set; }
        public string    Etat             { get; set; } = string.Empty;
    }
}