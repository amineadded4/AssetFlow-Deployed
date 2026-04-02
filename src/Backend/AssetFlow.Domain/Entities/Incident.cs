namespace AssetFlow.Domain.Entities
{
    public class Incident
    {
        public int Id { get; set; }
        public int AffectationId { get; set; }

        public Affectation Affectation { get; set; } = null!;
        public string TypeIncident { get; set; } = string.Empty;
        public int Urgence { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime DateIncident { get; set; } = DateTime.UtcNow;
        public StatutIncident Statut { get; set; } = StatutIncident.EnAttente;
        public DateTime? DateResolution { get; set; }
        public string? CommentairesResolution { get; set; }
        public int? ArticleId { get; set; }
        public ArticleIndividuel? Article { get; set; }
    }
    public enum StatutIncident
    {
        EnAttente,    // En attente de traitement
        EnCours,      // En cours de traitement
        Resolu,       // Résolu
        Cloture       // Clôturé (fermé définitivement)
    }
}