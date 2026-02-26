// ============================================================
// AssetFlow.Application / DTOs / IncidentDtos.cs
// DTOs pour la gestion des incidents
// ============================================================

namespace AssetFlow.Application.DTOs
{
    /// <summary>
    /// DTO pour signaler un nouvel incident
    /// </summary>
    public class SignalerIncidentRequestDto
    {
        public int AffectationId { get; set; }
        public int? ArticleId { get; set; }  // ← AJOUTER
        public string TypeIncident { get; set; } = string.Empty;
        public int Urgence { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Réponse après signalement d'incident
    /// </summary>
    public class SignalerIncidentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IncidentId { get; set; }
        public string? NumeroIncident { get; set; }
    }

    /// <summary>
    /// DTO représentant un incident pour l'affichage
    /// </summary>
    public class IncidentDto
    {
        public int Id { get; set; }
        public int AffectationId { get; set; }
        public string NumeroIncident { get; set; } = string.Empty;
        public string TypeIncident { get; set; } = string.Empty;
        public int Urgence { get; set; }
        public string UrgenceLabel { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DateIncident { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string StatutLabel { get; set; } = string.Empty;
        public DateTime? DateResolution { get; set; }
        public string? CommentairesResolution { get; set; }
        
        // Infos matériel
        public string MaterielDesignation { get; set; } = string.Empty;
        public string MaterielReference { get; set; } = string.Empty;
    }
}