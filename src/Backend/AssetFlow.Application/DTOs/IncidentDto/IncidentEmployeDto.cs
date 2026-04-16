namespace AssetFlow.Application.DTOs
{
    public class IncidentEmployeDto
    {
        public int    UtilisateurId { get; set; }
        public string FullName      { get; set; } = string.Empty;
        public string Role    { get; set; } = string.Empty;
        public string Initials      { get; set; } = string.Empty;
        public int    NbIncidentsActifs { get; set; }
    }
}