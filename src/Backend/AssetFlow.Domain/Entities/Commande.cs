// ============================================================
// AssetFlow.Domain / Entities / Commande.cs
// Représente une commande d'achat de matériel
// ============================================================

namespace AssetFlow.Domain.Entities
{
    public class Commande
    {
        public int Id { get; set; }

        /// <summary>Numéro de commande unique (ex: CMD-2026-001)</summary>
        public string NumeroCommande { get; set; } = string.Empty;

        /// <summary>FK → Materiel</summary>
        public int MaterielId { get; set; }
        public Materiel Materiel { get; set; } = null!;

        /// <summary>FK → Fournisseur</summary>
        public int FournisseurId { get; set; }
        public Fournisseur Fournisseur { get; set; } = null!;

        /// <summary>Quantité achetée dans cette commande</summary>
        public int QuantiteAchetee { get; set; }

        public DateTime DateAchat { get; set; } = DateTime.UtcNow;
        public DateTime? DateLivraison { get; set; }
        public DateTime? DateFinGarantie { get; set; }

        /// <summary>Articles individuels générés par cette commande</summary>
        public ICollection<ArticleIndividuel> Articles { get; set; } = new List<ArticleIndividuel>();
    }
}