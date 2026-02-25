// ============================================================
// AssetFlow.Domain / Entities / ArticleIndividuel.cs
// Un article physique individuel issu d'une commande
// ============================================================

namespace AssetFlow.Domain.Entities
{
    public class ArticleIndividuel
    {
        public int Id { get; set; }

        /// <summary>Numéro de série (optionnel, unique si renseigné)</summary>
        public string? NumeroSerie { get; set; }

        /// <summary>Statut de l'article</summary>
        public StatutArticle Statut { get; set; } = StatutArticle.Disponible;

        /// <summary>FK → Materiel</summary>
        public int MaterielId { get; set; }
        public Materiel Materiel { get; set; } = null!;

        /// <summary>FK → Commande</summary>
        public int CommandeId { get; set; }
        public Commande Commande { get; set; } = null!;
    }

    public enum StatutArticle
    {
        Disponible,
        Affecte,
        HorsService,
        EnReparation
    }
}