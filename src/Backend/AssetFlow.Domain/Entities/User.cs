namespace AssetFlow.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = true;
        public string? KeycloakId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// Keypoints faciaux sérialisés en JSON (float[][])
        /// Chaque keypoint = [x, y] normalisé entre 0 et 1
        /// 478 points MediaPipe FaceLandmarker
        /// null si l'utilisateur n'a pas enregistré son visage
        public string? FaceKeypoints { get; set; }
        public bool     ConsentIpTracking { get; set; } = false;
    }
}