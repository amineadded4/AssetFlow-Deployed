// ============================================================
// AssetFlow.Application / DTOs / AuthDtos.cs
// MISE À JOUR : Ajout de UserId dans LoginResponseDto
// ============================================================

namespace AssetFlow.Application.DTOs
{
    /// <summary>
    /// Données envoyées lors de la connexion
    /// </summary>
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // IT, EquipeAchat, Employe
    }

    /// <summary>
    /// Réponse retournée après une connexion réussie
    /// AJOUT : UserId pour identifier l'utilisateur côté frontend
    /// </summary>
    public class LoginResponseDto
    {
        public int UserId { get; set; }                    // ← NOUVEAU
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string Role { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Données envoyées lors de l'inscription
    /// </summary>
    public class RegisterRequestDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string RequestedRole { get; set; } = string.Empty; // IT, EquipeAchat, Employe
    }

    /// <summary>
    /// Réponse retournée après l'inscription
    /// </summary>
    public class RegisterResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}