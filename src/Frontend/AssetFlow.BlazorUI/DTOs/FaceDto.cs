namespace AssetFlow.BlazorUI.DTOs
{
    public class FaceLoginRequest
    {
        public string Email      { get; set; } = string.Empty;
        public float[][] Keypoints { get; set; } = Array.Empty<float[]>();
    }

    public class RegisterFaceRequest
    {
        public string Email      { get; set; } = string.Empty;
        public float[][] Keypoints { get; set; } = Array.Empty<float[]>();
    }
}