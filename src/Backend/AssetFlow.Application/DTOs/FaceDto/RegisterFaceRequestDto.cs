namespace AssetFlow.Application.DTOs
{
    public class RegisterFaceRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public float[][] Keypoints { get; set; } = Array.Empty<float[]>();
    }
}