// ============================================================
// AssetFlow.Domain / Entities / DemandeAchat.cs
// MODIF : Reference ajoutée dans LigneDemande
//
// MIGRATION SQL à exécuter dans SSMS :
//   ALTER TABLE LigneDemande ADD Reference VARCHAR(100) NULL;
// ============================================================

namespace AssetFlow.Domain.Entities
{
    public class DemandeAchat
    {
        public int      IdDemande    { get; set; }
        public string   Reference    { get; set; } = string.Empty; // référence auto de la demande
        public string   NomProduit   { get; set; } = string.Empty; // titre global
        public int      Quantite     { get; set; } = 1;
        public string?  Description  { get; set; }
        public string   Statut       { get; set; } = "en_attente";
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public string   DemandeurNom { get; set; } = string.Empty;
        public string?  MotifRefus   { get; set; }

        public List<OffreAchat>   Offres { get; set; } = new();
        public List<LigneDemande> Lignes { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────

    public class LigneDemande
    {
        public int     IdLigne     { get; set; }
        public int     IdDemande   { get; set; }
        public string  Reference   { get; set; } = string.Empty;   // ← référence par matériel
        public string  NomProduit  { get; set; } = string.Empty;
        public int     Quantite    { get; set; } = 1;
        public string? Description { get; set; }

        public DemandeAchat? Demande { get; set; }
    }

    // ─────────────────────────────────────────────────────────

    public class OffreAchat
    {
        public Guid    IdOffre     { get; set; } = Guid.NewGuid();
        public int     IdDemande   { get; set; }
        public string  NomFichier  { get; set; } = string.Empty;
        public long    Taille      { get; set; }
        public byte[]? ContenuPdf  { get; set; }
        public bool    EstChoisie  { get; set; } = false;
        public DemandeAchat? Demande { get; set; }
    }
}
