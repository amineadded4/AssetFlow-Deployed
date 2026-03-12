// ============================================================
// AssetFlow.Application / DTOs / DemandeAchatITDto.cs
// DTO dédié à la vue IT des demandes d'achat.
// Distinct du DTO agent achat pour garder la séparation
// des responsabilités (l'IT n'a pas besoin des offres, etc.)
// ============================================================

namespace AssetFlow.Application.DTOs
{
    public class DemandeAchatITDto
    {
        public int      IdDemande    { get; set; }
        public string   Reference    { get; set; } = string.Empty;
        public string   NomProduit   { get; set; } = string.Empty;
        public int      Quantite     { get; set; }
        public string?  Description  { get; set; }
        public string   Statut       { get; set; } = "en_attente";
        public DateTime DateCreation { get; set; }
        public string?  DemandeurNom { get; set; }
        public string?  MotifRefus   { get; set; }
    }
}
