namespace AssetFlow.BlazorUI.DTOs
{
    public class LoginRequest
    {
        public string Email    { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role     { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public int    UserId      { get; set; }
        public string AccessToken  { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int    ExpiresIn    { get; set; }
        public string Role         { get; set; } = string.Empty;
        public string FullName     { get; set; } = string.Empty;
        public string Email     { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string FirstName     { get; set; } = string.Empty;
        public string LastName      { get; set; } = string.Empty;
        public string Email         { get; set; } = string.Empty;
        public string Password      { get; set; } = string.Empty;
        public string Department    { get; set; } = string.Empty;
        public string RequestedRole { get; set; } = string.Empty;
    }

    public class RegisterResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}