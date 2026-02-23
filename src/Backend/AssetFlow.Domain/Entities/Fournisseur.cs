namespace AssetFlow.Domain.Entities
{
    /// <summary>
    /// Représente un fournisseur dans le système AssetFlow.
    /// Les propriétés correspondent aux colonnes de la table SQL.
    /// </summary>
    public class Fournisseur
    {
        /// <summary>Clé primaire — INT IDENTITY(1,1)</summary>
        public int IdFournisseur { get; set; }

        /// <summary>Nom du fournisseur — VARCHAR(100) NOT NULL</summary>
        public string Nom { get; set; } = string.Empty;

        /// <summary>Numéro de téléphone — VARCHAR(20) nullable</summary>
        public string? Telephone { get; set; }

        /// <summary>Adresse physique — VARCHAR(255) nullable</summary>
        public string? Adresse { get; set; }

        /// <summary>Adresse e-mail — VARCHAR(150) nullable</summary>
        public string? Mail { get; set; }

        // ─────────── Nouveaux champs ───────────

        /// <summary>Nombre total de commandes passées par ce fournisseur</summary>
        public int CommandesTotales { get; set; } = 0;

        /// <summary>Taux de livraison à temps (%) — valeur entre 0 et 100</summary>
        public decimal TauxLivraisonATemps { get; set; } = 0;

        /// <summary>Score de fiabilité (par exemple de 0 à 10)</summary>
        public decimal ScoreFiabilite { get; set; } = 0;

        /// <summary>Date de la dernière commande — nullable si aucune commande</summary>
        public DateTime? DerniereCommande { get; set; }
    }
}
        
        