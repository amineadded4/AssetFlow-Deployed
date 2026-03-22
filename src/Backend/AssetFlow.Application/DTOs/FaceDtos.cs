// ============================================================
// AssetFlow.Application / DTOs / FaceDtos.cs
// ============================================================

namespace AssetFlow.Application.DTOs
{
    /// <summary>
    /// Envoyé par le frontend pour login par visage
    /// </summary>
    public class FaceLoginRequestDto
    {
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 478 keypoints MediaPipe : [[x,y], [x,y], ...]
        /// </summary>
        public float[][] Keypoints { get; set; } = Array.Empty<float[]>();
    }

    /// <summary>
    /// Envoyé pour enregistrer le visage d'un utilisateur existant
    /// </summary>
    public class RegisterFaceRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public float[][] Keypoints { get; set; } = Array.Empty<float[]>();
    }
}