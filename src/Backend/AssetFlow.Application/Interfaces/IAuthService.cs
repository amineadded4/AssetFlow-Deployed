using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<LoginResponseDto?> RefreshAsync(string refreshToken);
        Task<bool> ForgotPasswordAsync(string email);
        Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request);
    }
}