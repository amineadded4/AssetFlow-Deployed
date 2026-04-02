namespace AssetFlow.Domain.Entities
{
    public class Project
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public StatutProjet Statut { get; set; } = StatutProjet.Planifie;
        public PrioriteProjet Priorite { get; set; } = PrioriteProjet.Moyenne;
        public string? Responsable { get; set; }
        public decimal? Budget { get; set; }
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum StatutProjet
    {
        Planifie  = 0,
        EnCours   = 1,
        Suspendu  = 2,
        Termine   = 3
    }

    public enum PrioriteProjet
    {
        Faible   = 0,
        Moyenne  = 1,
        Haute    = 2,
        Critique = 3
    }
}