// ============================================================
// AssetFlow.Infrastructure / Services / OffreAchatService.cs
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class OffreAchatService : IOffreAchatService
    {
        private readonly AppDbContext _context;

        public OffreAchatService(AppDbContext context)
        {
            _context = context;
        }

        // ── Lister les offres d'une demande ──────────────────────
        public async Task<List<OffreAchatDto>> GetByDemandeIdAsync(int demandeId)
        {
            return await _context.OffreAchat
                .Where(o => o.IdDemande == demandeId)
                .Select(o => new OffreAchatDto
                {
                    IdOffre    = o.IdOffre,
                    IdDemande  = o.IdDemande,
                    NomFichier = o.NomFichier,
                    Taille     = o.Taille,
                    EstChoisie = o.EstChoisie,
                    PrixTotal      = o.PrixTotal,
                    FraisLivraison = o.FraisLivraison,
                    DelaiLivraison = o.DelaiLivraison,
                    Garantie       = o.Garantie
                })
                .ToListAsync();
        }

        // ── Récupérer les octets PDF ──────────────────────────────
        public async Task<byte[]?> GetPdfBytesAsync(Guid offreId)
        {
            var offre = await _context.OffreAchat
                .Where(o => o.IdOffre == offreId)
                .Select(o => new { o.ContenuPdf })
                .FirstOrDefaultAsync();

            return offre?.ContenuPdf;
        }
        public async Task<bool> ChoisirOffreAsync(Guid offreId, int demandeId)
        {
            // S'assurer qu'aucune autre offre de cette demande n'est déjà choisie
            var offres = await _context.OffreAchat
                .Where(o => o.IdDemande == demandeId)
                .ToListAsync();

            foreach (var o in offres)
                o.EstChoisie = false;  // reset toutes

            var choisie = offres.FirstOrDefault(o => o.IdOffre == offreId);
            if (choisie == null) return false;

            choisie.EstChoisie = true;
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task SauvegarderInfosOcrAsync(Guid offreId, string? prix, string? frais, string? delai, string? garantie)
        {
            var offre = await _context.OffreAchat.FindAsync(offreId);
            if (offre == null) return;

            offre.PrixTotal      = prix;
            offre.FraisLivraison = frais;
            offre.DelaiLivraison = delai;
            offre.Garantie       = garantie;

            await _context.SaveChangesAsync();
        }
    }
}
