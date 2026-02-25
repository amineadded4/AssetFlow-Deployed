// ============================================================
// AssetFlow.Application / DTOs / CommandeDtos.cs
// ============================================================

namespace AssetFlow.Application.DTOs
{
    // ── Lecture ────────────────────────────────────────────────
    public class CommandeDto
    {
        public int Id { get; set; }
        public string NumeroCommande { get; set; } = string.Empty;
        public int MaterielId { get; set; }
        public string NomMateriel { get; set; } = string.Empty;
        public string ReferenceMateriel { get; set; } = string.Empty;
        public int FournisseurId { get; set; }
        public string NomFournisseur { get; set; } = string.Empty;
        public int QuantiteAchetee { get; set; }
        public DateTime DateAchat { get; set; }
        public DateTime? DateLivraison { get; set; }
        public DateTime? DateFinGarantie { get; set; }
        public List<ArticleDto> Articles { get; set; } = new();
    }

    public class ArticleDto
    {
        public int Id { get; set; }
        public string? NumeroSerie { get; set; }
        public string Statut { get; set; } = string.Empty;
        public int CommandeId { get; set; }
        public string NumeroCommande { get; set; } = string.Empty;
    }

    // ── Création ───────────────────────────────────────────────
    public class CreerCommandeDto
    {
        public string NumeroCommande { get; set; } = string.Empty;
        public int MaterielId { get; set; }
        public int FournisseurId { get; set; }
        public int QuantiteAchetee { get; set; }
        public DateTime DateAchat { get; set; } = DateTime.UtcNow;
        public DateTime? DateLivraison { get; set; }
        public DateTime? DateFinGarantie { get; set; }

        /// <summary>
        /// Liste des numéros de série (un par article, optionnel).
        /// Si vide ou moins d'entrées que QuantiteAchetee, le reste sera null.
        /// </summary>
        public List<string?> NumerosSerie { get; set; } = new();
    }

    // ── Réponse ────────────────────────────────────────────────
    public class CommandeReponseDto
    {
        public bool Succes { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IdCommande { get; set; }
    }

    // ── Vue enrichie pour le tableau Matériel ─────────────────
    public class MaterielAvecCommandeDto
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Categorie { get; set; } = string.Empty;
        public int QuantiteStock { get; set; }
        public int QuantiteMin { get; set; }
        public string Unite { get; set; } = "pièce";
        public string Etat { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime DateAjout { get; set; }

        // Infos de la dernière commande
        public string? NumeroCommande { get; set; }
        public string? NomFournisseur { get; set; }
        public int? FournisseurId { get; set; }
        public int QuantiteAchetee { get; set; }
        public DateTime? DateAchat { get; set; }
        public DateTime? DateLivraison { get; set; }
        public DateTime? DateFinGarantie { get; set; }

        // Résumé articles
        public int NbArticles { get; set; }
        public int NbDisponibles { get; set; }
    }
}