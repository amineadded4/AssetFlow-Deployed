namespace AssetFlow.Domain.Entities
{
    public class Affectation
    {
        public int Id { get; set; }
        public DateTime DateAffectation { get; set; } = DateTime.UtcNow;
        public int QuantiteAffectee { get; set; }
        public int QuantiteRetournee { get; set; } = 0;
        public DateTime? DateRetour { get; set; }
        public string? Observations { get; set; }

        // === RELATIONS ===
        public int MaterielId { get; set; }
        public Materiel Materiel { get; set; } = null!;

        public int? UtilisateurId { get; set; }
        public User? Utilisateur { get; set; } = null!;

        public List<ArticleIndividuel> Articles { get; set; } = new();
        public EtatAffectation Etat { get; set; } = EtatAffectation.Courante;

        public int? ProjetId { get; set; }
        public Project? Projet { get; set; }
    }

    public enum EtatAffectation
    {
        Courante = 0,
        Terminee = 1
    }
}