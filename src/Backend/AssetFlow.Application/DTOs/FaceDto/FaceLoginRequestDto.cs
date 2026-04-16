namespace AssetFlow.Application.DTOs
{
    public class FaceLoginRequestDto
    {
        public string Email { get; set; } = string.Empty;

        /// 478 keypoints MediaPipe : [[x,y], [x,y], ...]
        public float[][] Keypoints { get; set; } = Array.Empty<float[]>();
    }
}