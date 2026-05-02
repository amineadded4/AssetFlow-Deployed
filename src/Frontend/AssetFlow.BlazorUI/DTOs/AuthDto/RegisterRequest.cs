namespace AssetFlow.BlazorUI.DTOs
{
    public class RegisterRequest
    {
        public string FirstName     { get; set; } = string.Empty;
        public string LastName      { get; set; } = string.Empty;
        public string Email         { get; set; } = string.Empty;
        public string Password      { get; set; } = string.Empty;
        public string Department    { get; set; } = string.Empty;
        public string RequestedRole { get; set; } = string.Empty;
        public bool   ConsentIpTracking { get; set; } = false;
    }
}