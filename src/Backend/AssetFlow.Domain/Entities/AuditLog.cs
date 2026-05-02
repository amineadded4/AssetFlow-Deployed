namespace AssetFlow.Domain.Entities
{
    public class AuditLog
    {
        public int      Id           { get; set; }
        public DateTime Timestamp    { get; set; } = DateTime.UtcNow;
        public string   Utilisateur  { get; set; } = string.Empty; // prénom + nom
        public string   Email        { get; set; } = string.Empty;
        public string   Action       { get; set; } = string.Empty; // CREATION, MODIFICATION, SUPPRESSION, CONNEXION, VALIDATION...
        public string   Categorie    { get; set; } = string.Empty; // Inscription, Matériel, Affectation, DemandeAchat
        public string   Entite       { get; set; } = string.Empty; // ex: "Matériel #REF-001", "Affectation #12"
        public string?  Details      { get; set; }                 // description courte
        public int?     UserId       { get; set; }
        public User?    User         { get; set; }
        public string?  IpAddress    { get; set; }
        public string?  GeoLocation  { get; set; }
    }
}