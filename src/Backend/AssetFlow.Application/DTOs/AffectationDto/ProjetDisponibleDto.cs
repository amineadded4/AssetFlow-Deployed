namespace AssetFlow.Application.DTOs
{
    public class ProjetDisponibleDto
    {
        public int    Id          { get; set; }
        public string Nom         { get; set; } = string.Empty;
        public string Statut      { get; set; } = string.Empty;
        public string Priorite    { get; set; } = string.Empty;
        public string? Responsable { get; set; }
    }
}