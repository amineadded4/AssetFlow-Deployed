namespace AssetFlow.Application.DTOs
{
    public class FaceLoginRequestDto
    {
        public string Email { get; set; } = string.Empty;

        /// 478 keypoints MediaPipe : [[x,y], [x,y], ...]
        public float[][] Keypoints { get; set; } = Array.Empty<float[]>();
    }

    /// Envoyé pour enregistrer le visage d'un utilisateur existant
    public class RegisterFaceRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public float[][] Keypoints { get; set; } = Array.Empty<float[]>();
    }
}