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
                    EstChoisie = o.EstChoisie
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
    }
}
