namespace AssetFlow.Application.DTOs
{
    public class EmployeListeDto
    {
        public int    Id         { get; set; }
        public string FullName   { get; set; } = string.Empty;
        public string Email      { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Initials   { get; set; } = string.Empty;
        public int    NbAffectationsActives { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}