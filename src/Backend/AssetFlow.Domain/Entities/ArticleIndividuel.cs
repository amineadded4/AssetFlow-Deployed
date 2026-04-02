namespace AssetFlow.Domain.Entities
{
    public class ArticleIndividuel
    {
        public int Id { get; set; }

        /// <summary>Numéro de série (optionnel, unique si renseigné)</summary>
        public string? NumeroSerie { get; set; }
        public StatutArticle Statut { get; set; } = StatutArticle.Disponible;

        /// <summary>FK → Materiel</summary>
        public int MaterielId { get; set; }
        public Materiel Materiel { get; set; } = null!;

        /// <summary>FK → Commande</summary>
        public int CommandeId { get; set; }
        public Commande Commande { get; set; } = null!;
        public EtatArticle Etat { get; set; } = EtatArticle.Bon;  // ← AJOUTER
        public int? AffectationId { get; set; }
        public Affectation? Affectation { get; set; }

    }

    public enum StatutArticle
    {
        Disponible,
        Affecte,
        HorsService,
        EnReparation
    }
    public enum EtatArticle
    {
        Bon = 0,
        Panne = 1
    }
}