// ============================================================
// AssetFlow.Application / Interfaces / IFaceAuthService.cs
// ============================================================

using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IFaceAuthService
    {
        /// <summary>
        /// Authentifie un utilisateur par reconnaissance faciale.
        /// Compare les keypoints reçus avec ceux stockés en base.
        /// </summary>
        Task<LoginResponseDto?> FaceLoginAsync(FaceLoginRequestDto request);

        /// <summary>
        /// Enregistre les keypoints faciaux d'un utilisateur existant.
        /// </summary>
        Task<bool> RegisterFaceAsync(RegisterFaceRequestDto request);
    }
}