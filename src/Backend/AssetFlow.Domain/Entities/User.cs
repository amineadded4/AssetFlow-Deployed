// ============================================================
// AssetFlow.Domain / Entities / User.cs
// Couche Domain : entité principale User (pas de dépendances externes)
// ============================================================

namespace AssetFlow.Domain.Entities
{
    /// <summary>
    /// Représente un utilisateur de l'application AssetFlow
    /// </summary>
    public class User
    {
        public int Id { get; set; }

        /// <summary>Prénom de l'utilisateur</summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Nom de famille</summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>Email professionnel</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Département (ex: IT, Production, Logistique)</summary>
        public string Department { get; set; } = string.Empty;

        /// <summary>Rôle demandé : IT, EquipeAchat, Employe</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>Indique si le compte est activé par l'admin</summary>
        public bool IsApproved { get; set; } = true;

        /// <summary>ID utilisateur dans Keycloak</summary>
        public string? KeycloakId { get; set; }

        /// <summary>Date de création du compte</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Keypoints faciaux sérialisés en JSON (float[][])
        /// Chaque keypoint = [x, y] normalisé entre 0 et 1
        /// 478 points MediaPipe FaceLandmarker
        /// null si l'utilisateur n'a pas enregistré son visage
        /// </summary>
        public string? FaceKeypoints { get; set; }
    }
}