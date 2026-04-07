namespace AssetFlow.Domain.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public TypeNotification Type { get; set; } = TypeNotification.AffectationExpiree;
        public NiveauNotification Niveau { get; set; } = NiveauNotification.Avertissement;
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public DateTime? DateLecture { get; set; }

        public bool EstLue { get; set; } = false;

        // === Liens optionnels ===
        /// <summary>ID de l'affectation concernée (si applicable)</summary>
        public int? AffectationId { get; set; }
        public Affectation? Affectation { get; set; }

        /// <summary>ID de l'employé concerné (si applicable)</summary>
        public int? UtilisateurId { get; set; }
        public User? Utilisateur { get; set; }

        /// <summary>Rôle destinataire (IT, Admin, etc.) — null = tous</summary>
        public string? RoleDestinataire { get; set; }
    }

    public enum TypeNotification
    {
        AffectationExpiree   = 0,
        RetourEnRetard       = 1,
        StockBas             = 2,
        NouvelleAffectation  = 3,
        Incident             = 4,
        Systeme              = 5
    }

    public enum NiveauNotification
    {
        Info         = 0,
        Avertissement = 1,
        Critique     = 2
    }
}