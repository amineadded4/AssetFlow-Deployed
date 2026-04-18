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
        private readonly IDashboardNotifier _notifier;
        private readonly IAuditLogService _audit;

        public DemandeAchatITService(AppDbContext context, IDashboardNotifier notifier, IAuditLogService audit)
        {
            _context  = context;
            _notifier = notifier;
            _audit    = audit;
        }

        public async Task<IEnumerable<DemandeAchatITDto>> GetAllAsync(int? userId)
        {
            var query = _context.DemandeAchat.Include(d => d.Lignes).AsQueryable();
            if (userId.HasValue) query = query.Where(d => d.UserId == userId.Value);
            return await query.OrderByDescending(d => d.DateCreation).Select(d => ToDto(d)).ToListAsync();
        }

        public async Task<DemandeAchatITDto?> GetByIdAsync(int id)
        {
            var d = await _context.DemandeAchat.Include(d => d.Lignes).FirstOrDefaultAsync(d => d.IdDemande == id);
            return d == null ? null : ToDto(d);
        }

        public async Task<DemandeAchatITDto> CreateAsync(CreateDemandeAchatDto dto)
        {
            if (dto.Lignes == null || !dto.Lignes.Any())
                throw new ArgumentException("Au moins une ligne de matériel est obligatoire.");

            var referenceGlobale = $"SN-{DateTime.Now:yyyy}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

            var demande = new DemandeAchat
            {
                UserId       = dto.UserId,
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
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();
            // ── MEMORY ──────────────────────────────────────────────────────────
            await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
            {
                Type   = "demande",
                NodeId = $"d-{demande.IdDemande}"
            });
            // ────────────────────────────────────────────────────────────────────
            await _audit.LogAsync(new CreateAuditLogDto
            {
                Utilisateur = dto.Utilisateur,
                Email       = "system",
                Action      = IAuditLogService.Actions.Creation,
                Categorie   = IAuditLogService.Categories.DemandeAchat,
                Entite      = $"Demande d'achat #{demande.Reference}",
                Details     = $"Nouvelle demande d'achat créée : \"{demande.NomProduit}\" (Qté: {demande.Quantite})"
            });
            return ToDto(demande);
        }

        public async Task<DemandeAchatITDto?> UpdateAsync(int id, UpdateDemandeAchatDto dto)
        {
            var demande = await _context.DemandeAchat
                .Include(d => d.Lignes)
                .FirstOrDefaultAsync(d => d.IdDemande == id);
            if (demande == null) return null;

            demande.NomProduit  = dto.NomProduit.Trim();
            demande.Description = dto.Description?.Trim();

            if (dto.Lignes != null && dto.Lignes.Any())
            {
                _context.Set<LigneDemande>().RemoveRange(demande.Lignes);
                demande.Lignes = dto.Lignes.Select(l => new LigneDemande
                {
                    IdDemande   = id,
                    Reference   = l.Reference?.Trim() ?? string.Empty,
                    NomProduit  = l.NomProduit.Trim(),
                    Quantite    = l.Quantite,
                    Description = l.Description?.Trim()
                }).ToList();
                demande.Quantite = dto.Lignes.Sum(l => l.Quantite);
            }

            await _context.SaveChangesAsync();
            // ── MEMORY ──────────────────────────────────────────────────────────
            await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
            {
                Type   = "demande",
                NodeId = $"d-{id}"
            });
            // ────────────────────────────────────────────────────────────────────
            await _audit.LogAsync(new CreateAuditLogDto
            {
                Utilisateur = dto.Utilisateur,
                Email       = "system",
                Action      = IAuditLogService.Actions.Modification,
                Categorie   = IAuditLogService.Categories.DemandeAchat,
                Entite      = $"Demande d'achat #{demande.Reference}",
                Details     = $"Demande d'achat mise à jour : \"{demande.NomProduit}\" (Qté: {demande.Quantite})"
            });
            return ToDto(demande);
        }

        public async Task<bool> DeleteAsync(int id, string userName)
        {
            var demande = await _context.DemandeAchat
                .Include(d => d.Lignes)
                .Include(d => d.Offres)
                .FirstOrDefaultAsync(d => d.IdDemande == id);
            if (demande == null) return false;

            if (demande.Offres.Any())  _context.Set<OffreAchat>().RemoveRange(demande.Offres);
            if (demande.Lignes.Any())  _context.Set<LigneDemande>().RemoveRange(demande.Lignes);

            _context.DemandeAchat.Remove(demande);
            await _context.SaveChangesAsync();
            // ── MEMORY ──────────────────────────────────────────────────────────
            // On notifie un refresh global des demandes (l'entité n'existe plus)
            await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
            {
                Type   = "demande",
                NodeId = $"d-{id}"
            });
            // ────────────────────────────────────────────────────────────────────
            await _audit.LogAsync(new CreateAuditLogDto
            {
                Utilisateur = userName,
                Email       = "system",
                Action      = IAuditLogService.Actions.Suppression,
                Categorie   = IAuditLogService.Categories.Materiel,
                Entite      = $"Matériel #{demande.Reference}",
                Details     = $"Supprimé avec cascade : \"{demande.NomProduit}\""
            });
            return true;
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