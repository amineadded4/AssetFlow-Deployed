using AssetFlow.Domain.Entities;

namespace AssetFlow.Application.Interfaces
{
    public interface IDemandeAchatService
    {
        Task<List<DemandeAchat>> GetAllAsync();

        Task<DemandeAchat?> GetByIdAsync(int id);
        Task ChangerStatutAsync(int idDemande, string statut, string? motifRefus = null);

        // ── Offres PDF ───────────────────────────────────────────

        Task<OffreAchat> AjouterOffreAsync(int idDemande, OffreAchat offre);

        Task SupprimerOffreAsync(Guid idOffre);

        // Retourne le binaire PDF pour l'aperçu / téléchargement
        Task<byte[]?> GetContenuPdfAsync(Guid idOffre);
    }
}
