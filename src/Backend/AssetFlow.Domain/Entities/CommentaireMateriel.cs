// ============================================================
// AssetFlow.Domain / Entities / CommentaireMateriel.cs
// ============================================================

namespace AssetFlow.Domain.Entities
{
    public class CommentaireMateriel
    {
        public int      Id            { get; set; }
        public int      MaterielId    { get; set; }
        public int      UtilisateurId { get; set; }
        public string   Contenu       { get; set; } = string.Empty;
        public DateTime DateCreation  { get; set; } = DateTime.UtcNow;

        // Navigation
        public Materiel Materiel    { get; set; } = null!;
        public User     Utilisateur { get; set; } = null!;
    }
}
