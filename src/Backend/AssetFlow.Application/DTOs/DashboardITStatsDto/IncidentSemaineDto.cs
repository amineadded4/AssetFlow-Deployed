namespace AssetFlow.Application.DTOs
{
    public class IncidentSemaineDto
    {
        public string Label    { get; set; } = string.Empty;
        public int    EnAttente { get; set; }
        public int    EnCours  { get; set; }
        public int    Resolu   { get; set; }
    }
}