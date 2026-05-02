namespace AssetFlow.Application.DTOs
{
    public class RegisterRequestDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string RequestedRole { get; set; } = string.Empty; // IT, EquipeAchat, Employe
        public bool   ConsentIpTracking { get; set; } = false;
    }
}