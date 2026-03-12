// ============================================================
// AssetFlow.Infrastructure / Services / DemandeAchatITService.cs
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class DemandeAchatITService : IDemandeAchatITService
    {
        private readonly AppDbContext _context;

        public DemandeAchatITService(AppDbContext context)
        {
            _context = context;
        }

        // ── GET ALL ──────────────────────────────────────────────
        public async Task<IEnumerable<DemandeAchatITDto>> GetAllAsync()
        {
            return await _context.DemandeAchat
                .OrderByDescending(d => d.DateCreation)
                .Select(d => ToDto(d))
                .ToListAsync();
        }

        // ── GET BY ID ────────────────────────────────────────────
        public async Task<DemandeAchatITDto?> GetByIdAsync(int id)
        {
            var d = await _context.DemandeAchat.FindAsync(id);
            return d == null ? null : ToDto(d);
        }

        // ── CREATE ───────────────────────────────────────────────
        public async Task<DemandeAchatITDto> CreateAsync(CreateDemandeAchatDto dto)
        {
            // Génère une référence unique : ex. "SN-2026-0042"
            // Utilise la référence fournie ou en génère une automatiquement
            var reference = !string.IsNullOrWhiteSpace(dto.Reference)
                ? dto.Reference.Trim()
                : $"SN-{DateTime.Now:yyyy}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

            var demande = new DemandeAchat
            {
                Reference    = reference,
                NomProduit   = dto.NomProduit.Trim(),
                Quantite     = dto.Quantite,
                Description  = dto.Description?.Trim(),
                DemandeurNom = dto.DemandeurNom ?? "IT",
                Statut       = "en_attente",
                DateCreation = DateTime.Now,
                MotifRefus   = null
            };

            _context.DemandeAchat.Add(demande);
            await _context.SaveChangesAsync();

            return ToDto(demande);
        }

        // ── Mapper ───────────────────────────────────────────────
        private static DemandeAchatITDto ToDto(DemandeAchat d) => new()
        {
            IdDemande    = d.IdDemande,
            Reference    = d.Reference,
            NomProduit   = d.NomProduit,
            Quantite     = d.Quantite,
            Description  = d.Description,
            Statut       = d.Statut,
            DateCreation = d.DateCreation,
            DemandeurNom = d.DemandeurNom,
            MotifRefus   = d.MotifRefus
        };
    }
}
