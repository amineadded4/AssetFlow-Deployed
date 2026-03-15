// ============================================================
// AssetFlow.Infrastructure / Services / DemandeAchatITService.cs
// MODIF : Reference par ligne de matériel
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

        public async Task<IEnumerable<DemandeAchatITDto>> GetAllAsync()
        {
            return await _context.DemandeAchat
                .Include(d => d.Lignes)
                .OrderByDescending(d => d.DateCreation)
                .Select(d => ToDto(d))
                .ToListAsync();
        }

        public async Task<DemandeAchatITDto?> GetByIdAsync(int id)
        {
            var d = await _context.DemandeAchat
                .Include(d => d.Lignes)
                .FirstOrDefaultAsync(d => d.IdDemande == id);
            return d == null ? null : ToDto(d);
        }

        public async Task<DemandeAchatITDto> CreateAsync(CreateDemandeAchatDto dto)
        {
            if (dto.Lignes == null || !dto.Lignes.Any())
                throw new ArgumentException("Au moins une ligne de matériel est obligatoire.");

            // Référence globale auto-générée pour la demande
            var referenceGlobale = $"SN-{DateTime.Now:yyyy}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

            var demande = new DemandeAchat
            {
                Reference    = referenceGlobale,
                NomProduit   = string.IsNullOrWhiteSpace(dto.NomProduit)
                                ? dto.Lignes.First().NomProduit.Trim()
                                : dto.NomProduit.Trim(),
                Quantite     = dto.Lignes.Sum(l => l.Quantite),
                Description  = dto.Description?.Trim(),
                DemandeurNom = dto.DemandeurNom ?? "IT",
                Statut       = "en_attente",
                DateCreation = DateTime.Now,
                Lignes       = dto.Lignes.Select(l => new LigneDemande
                {
                    Reference   = l.Reference?.Trim() ?? string.Empty,
                    NomProduit  = l.NomProduit.Trim(),
                    Quantite    = l.Quantite,
                    Description = l.Description?.Trim()
                }).ToList()
            };

            _context.DemandeAchat.Add(demande);
            await _context.SaveChangesAsync();

            return ToDto(demande);
        }

        private static DemandeAchatITDto ToDto(DemandeAchat d) => new()
        {
            IdDemande    = d.IdDemande,
            Reference    = d.Reference,
            NomProduit   = d.NomProduit,
            Quantite     = d.Lignes.Any() ? d.Lignes.Sum(l => l.Quantite) : d.Quantite,
            Description  = d.Description,
            Statut       = d.Statut,
            DateCreation = d.DateCreation,
            DemandeurNom = d.DemandeurNom,
            MotifRefus   = d.MotifRefus,
            Lignes       = d.Lignes.Select(l => new LigneDemandeDto
            {
                IdLigne     = l.IdLigne,
                Reference   = l.Reference,
                NomProduit  = l.NomProduit,
                Quantite    = l.Quantite,
                Description = l.Description
            }).ToList()
        };
    }
}
