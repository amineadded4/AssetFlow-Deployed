using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IFaceAuthService
    {
        // Authentifie un utilisateur par reconnaissance faciale.
        // Compare les keypoints reçus avec ceux stockés en base.
        Task<LoginResponseDto?> FaceLoginAsync(FaceLoginRequestDto request);

        // Enregistre les keypoints faciaux d'un utilisateur existant.
        Task<bool> RegisterFaceAsync(RegisterFaceRequestDto request);
    }
}