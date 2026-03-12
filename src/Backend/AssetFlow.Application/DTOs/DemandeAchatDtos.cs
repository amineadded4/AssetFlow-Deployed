// ============================================================
// COUCHE  : AssetFlow.Application
// FICHIER : DTOs/DemandeAchatDtos.cs
// RÔLE    : Objets de transfert entre Controller et Frontend.
//           Même pattern que FournisseurDtos.cs
// ============================================================

namespace AssetFlow.Application.DTOs
{
    // ── LECTURE ─────────────────────────────────────────────────

    /// <summary>
    /// DTO complet d'une demande retourné par l'API.
    /// Inclut les offres SANS le binaire PDF (trop lourd).
    /// Le PDF est téléchargé séparément via GET .../pdf
    /// </summary>
    public class DemandeAchatDto
    {
        public int              IdDemande    { get; set; }
        public string           Reference    { get; set; } = string.Empty;
        public string           NomProduit   { get; set; } = string.Empty;
        public int              Quantite     { get; set; }
        public string?          Description  { get; set; }
        public string           Statut       { get; set; } = string.Empty;
        public DateTime         DateCreation { get; set; }
        public string           DemandeurNom { get; set; } = string.Empty;
        public string?          MotifRefus   { get; set; }
        public List<OffreAchatDto> Offres    { get; set; } = new();
    }

    /// <summary>
    /// DTO d'une offre PDF — sans le binaire.
    /// EstChoisie = true → badge "Choisie par l'IT" affiché dans le frontend.
    /// </summary>
    /*public class OffreAchatDto
    {
        public Guid   IdOffre    { get; set; }
        public string NomFichier { get; set; } = string.Empty;
        public long   Taille     { get; set; }
        public bool   EstChoisie { get; set; }
    }*/

    // ── CHANGER STATUT ───────────────────────────────────────────

    /// <summary>
    /// DTO reçu par PUT /api/demandes/{id}/statut
    /// Statuts valides : en_attente | commande | traite | refuse
    /// MotifRefus obligatoire si Statut == "refuse"
    /// </summary>
    public class ChangerStatutDto
    {
        public string  Statut     { get; set; } = string.Empty;
        public string? MotifRefus { get; set; }
    }

    // ── RÉPONSE STANDARD ────────────────────────────────────────

    /// <summary>
    /// Réponse standard pour toutes les opérations d'écriture.
    /// Même pattern que FournisseurReponseDto.
    /// </summary>
    public class DemandeAchatReponseDto
    {
        public bool   Succes    { get; set; }
        public string Message   { get; set; } = string.Empty;
        public int?   IdDemande { get; set; }
    }
}
