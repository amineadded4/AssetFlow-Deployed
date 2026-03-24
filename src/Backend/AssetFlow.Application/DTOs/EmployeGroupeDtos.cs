// ============================================================
// AssetFlow.Application / DTOs / EmployeGroupeDtos.cs
// DTOs pour l'affichage groupé matériel → articles
// ============================================================

namespace AssetFlow.Application.DTOs
{
    /// <summary>
    /// DTO représentant un article individuel affecté à un employé
    /// (ligne dans le modal)
    /// </summary>
    /*public class ArticleAffecteDto
    {
        /// <summary>ID de l'affectation</summary>
        public int AffectationId { get; set; }

        /// <summary>ID de l'article individuel</summary>
        public int ArticleId { get; set; }

        /// <summary>Numéro de série de l'article (ex: SN-5592-X)</summary>
        public string NumeroSerie { get; set; } = string.Empty;

        /// <summary>Statut de l'article (Disponible, Affecte, HorsService, EnReparation)</summary>
        public string StatutArticle { get; set; } = string.Empty;
        public string EtatArticle { get; set; } = "Bon";        // ← AJOUTER


        /// <summary>Couleur du badge statut affectation</summary>
        public string StatutBadgeColor { get; set; } = string.Empty;

        /// <summary>Date d'affectation de cet article</summary>
        public DateTime DateAffectation { get; set; }

        /// <summary>Observations sur cet article</summary>
        public string? Observations { get; set; }
    }*/

    /// <summary>
    /// DTO représentant un matériel groupé avec tous ses articles affectés
    /// à l'employé (pour la grille principale)
    /// </summary>
    /*public class MaterielAffecteGroupeDto
    {
        /// <summary>ID du matériel</summary>
        public int MaterielId { get; set; }

        /// <summary>Référence du matériel (ex: REF-LAPTOP-001)</summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>Désignation (ex: Laptop Dell Latitude)</summary>
        public string Designation { get; set; } = string.Empty;

        /// <summary>Catégorie (ex: Informatique)</summary>
        public string Categorie { get; set; } = string.Empty;

        /// <summary>URL de l'image</summary>
        public string? ImageUrl { get; set; }

        /// <summary>Nombre total d'articles affectés à cet employé pour ce matériel</summary>
        public int NombreArticles { get; set; }

        /// <summary>Statut dominant (le plus représenté parmi les articles)</summary>
        public string StatutDominant { get; set; } = string.Empty;

        /// <summary>Couleur du badge statut dominant</summary>
        public string StatutBadgeColor { get; set; } = string.Empty;

        /// <summary>Date de la plus récente affectation</summary>
        public DateTime DerniereAffectation { get; set; }

        /// <summary>Liste des articles individuels affectés</summary>
        public List<ArticleAffecteDto> Articles { get; set; } = new();
    }*/
}