namespace AssetFlow.Application.DTOs
{
    public class CreateProjectDto
    {
        public string   Nom         { get; set; } = string.Empty;
        public string?  Description { get; set; }
        public string   Statut      { get; set; } = "Planifie";
        public string   Priorite    { get; set; } = "Moyenne";
        public string?  Responsable { get; set; }
        public decimal? Budget      { get; set; }
        public DateTime? DateDebut  { get; set; }
        public DateTime? DateFin    { get; set; }
    }
}