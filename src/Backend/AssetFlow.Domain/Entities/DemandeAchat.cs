// ============================================================
// COUCHE  : AssetFlow.Domain
// FICHIER : Entities/DemandeAchat.cs
// RÔLE    : Entités métier pures — aucune dépendance externe.
//
// SCRIPT SQL À EXÉCUTER DANS SQL SERVER MANAGEMENT STUDIO :
// (pour remplir manuellement les données de test)
//
// CREATE TABLE DemandeAchat (
//     IdDemande     INT IDENTITY(1,1) PRIMARY KEY,
//     Reference     VARCHAR(30)   NOT NULL,
//     NomProduit    VARCHAR(200)  NOT NULL,
//     Quantite      INT           NOT NULL DEFAULT 1,
//     Description   NVARCHAR(MAX) NULL,
//     Statut        VARCHAR(20)   NOT NULL DEFAULT 'en_attente',
//     DateCreation  DATETIME2     NOT NULL DEFAULT GETDATE(),
//     DemandeurNom  VARCHAR(150)  NOT NULL,
//     MotifRefus    NVARCHAR(500) NULL
// );
//
// CREATE TABLE OffreAchat (
//     IdOffre      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
//     IdDemande    INT NOT NULL REFERENCES DemandeAchat(IdDemande) ON DELETE CASCADE,
//     NomFichier   VARCHAR(300)  NOT NULL,
//     Taille       BIGINT        NOT NULL,
//     ContenuPdf   VARBINARY(MAX) NULL,
//     EstChoisie   BIT           NOT NULL DEFAULT 0
//     -- EstChoisie = 1 quand l'IT a sélectionné cette offre
// );
//
// -- DONNÉES DE TEST (à coller dans SSMS) :
// INSERT INTO DemandeAchat (Reference, NomProduit, Quantite, Description, Statut, DemandeurNom)
// VALUES
//   ('#PR-2024-042', 'RAM DDR4 32GB ECC', 4, 'Extension mémoire serveur SRV-DB-01', 'en_attente', 'Marc Lefebvre'),
//   ('#PR-2024-039', 'MacBook Pro 14" M3 Max', 1, 'Remplacement poste développeur mobile', 'en_attente', 'Sophie Martin'),
//   ('#PR-2024-035', 'Switch Cisco 48 ports', 1, 'Nouveau plateau open space', 'commande', 'Marc Lefebvre'),
//   ('#PR-2024-031', 'Écran Dell UltraSharp 32"', 2, 'Postes designers UX', 'traite', 'Thomas Bernard'),
//   ('#PR-2024-028', 'Clavier Logitech MX Keys', 5, 'Renouvellement open space RH', 'refuse', 'Léa Dubois');
//
// UPDATE DemandeAchat SET MotifRefus = 'Budget épuisé ce trimestre, reporter Q1 2025.'
// WHERE Reference = '#PR-2024-028';
// ============================================================

namespace AssetFlow.Domain.Entities
{
    /// <summary>
    /// Demande d'achat créée par un Agent IT.
    /// L'Agent Achat gère son statut et y attache des offres PDF.
    /// </summary>
    public class DemandeAchat
    {
        public int      IdDemande    { get; set; }

        /// <summary>Référence unique — ex: #PR-2024-042</summary>
        public string   Reference    { get; set; } = string.Empty;

        /// <summary>Nom du produit demandé</summary>
        public string   NomProduit   { get; set; } = string.Empty;

        /// <summary>Quantité demandée</summary>
        public int      Quantite     { get; set; } = 1;

        /// <summary>Description détaillée du besoin</summary>
        public string?  Description  { get; set; }

        /// <summary>
        /// Statut courant — valeurs possibles :
        /// en_attente | commande | traite | refuse
        /// </summary>
        public string   Statut       { get; set; } = "en_attente";

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        /// <summary>Nom de l'Agent IT qui a créé la demande</summary>
        public string   DemandeurNom { get; set; } = string.Empty;

        /// <summary>Motif saisi par l'Agent Achat lors d'un refus</summary>
        public string?  MotifRefus   { get; set; }

        // Navigation EF Core
        public List<OffreAchat> Offres { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Offre PDF attachée par l'Agent Achat à une demande.
    /// EstChoisie = true quand l'IT a sélectionné cette offre.
    /// </summary>
    
    public class OffreAchat
    {
        /// <summary>Clé primaire GUID</summary>
        public Guid    IdOffre     { get; set; } = Guid.NewGuid();

        /// <summary>FK vers DemandeAchat</summary>
        public int     IdDemande   { get; set; }

        /// <summary>Nom original du fichier PDF</summary>
        public string  NomFichier  { get; set; } = string.Empty;

        /// <summary>Taille en octets</summary>
        public long    Taille      { get; set; }

        /// <summary>Contenu binaire du PDF — VARBINARY(MAX)</summary>
        public byte[]? ContenuPdf  { get; set; }

        /// <summary>
        /// True = l'Agent IT a choisi cette offre.
        /// L'Agent Achat le voit pour savoir quelle offre a été retenue.
        /// </summary>
        public bool    EstChoisie  { get; set; } = false;

        // Navigation inverse
        public DemandeAchat? Demande { get; set; }
    }
}
