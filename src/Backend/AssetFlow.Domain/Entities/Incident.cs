// ============================================================
// AssetFlow.Domain / Entities / Incident.cs
// Entité représentant un incident signalé sur un équipement
// ============================================================

namespace AssetFlow.Domain.Entities
{
    /// <summary>
    /// Représente un incident signalé par un employé sur un équipement affecté
    /// </summary>
    public class Incident
    {
        /// <summary>Identifiant unique</summary>
        public int Id { get; set; }

        /// <summary>ID de l'affectation concernée</summary>
        public int AffectationId { get; set; }

        /// <summary>Navigation : affectation concernée</summary>
        public Affectation Affectation { get; set; } = null!;

        /// <summary>Type d'incident (Panne, Casse, Vol, Autre)</summary>
        public string TypeIncident { get; set; } = string.Empty;

        /// <summary>Niveau d'urgence (0-100)</summary>
        public int Urgence { get; set; }

        /// <summary>Description détaillée de l'incident</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Date de l'incident</summary>
        public DateTime DateIncident { get; set; } = DateTime.UtcNow;

        /// <summary>Statut du traitement (EnAttente, EnCours, Resolu, Cloture)</summary>
        public StatutIncident Statut { get; set; } = StatutIncident.EnAttente;

        /// <summary>Date de résolution (si résolu)</summary>
        public DateTime? DateResolution { get; set; }

        /// <summary>Commentaires de résolution (équipe IT)</summary>
        public string? CommentairesResolution { get; set; }
        public int? ArticleId { get; set; }
        public ArticleIndividuel? Article { get; set; }
    }

    /// <summary>
    /// Statuts possibles d'un incident
    /// </summary>
    public enum StatutIncident
    {
        EnAttente,    // En attente de traitement
        EnCours,      // En cours de traitement
        Resolu,       // Résolu
        Cloture       // Clôturé (fermé définitivement)
    }
}