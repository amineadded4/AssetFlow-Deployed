using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Application.DTOs;

namespace AssetFlow.Infrastructure.Services
{
    public class DemandeAchatService : IDemandeAchatService
    {
        private readonly AppDbContext _context;
        private readonly IDashboardNotifier _notifier;
        private readonly IAuditLogService _audit;

        public DemandeAchatService(AppDbContext context, IDashboardNotifier notifier, IAuditLogService audit)
        {
            _context = context;
            _notifier = notifier;
            _audit = audit;
        }

        public async Task<List<DemandeAchat>> GetAllAsync()
        {
            return await _context.DemandeAchat
                .Include(d => d.Offres)
                .Include(d => d.Lignes)
                .OrderByDescending(d => d.DateCreation)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<DemandeAchat?> GetByIdAsync(int id)
        {
            return await _context.DemandeAchat
                .Include(d => d.Offres)
                .Include(d => d.Lignes)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.IdDemande == id);
        }

        public async Task ChangerStatutAsync(
            int idDemande, string statut, string userName,string? motifRefus = null)
        {
            var demande = await _context.DemandeAchat
                .FirstOrDefaultAsync(d => d.IdDemande == idDemande);

            if (demande == null)
                throw new KeyNotFoundException($"Demande ID {idDemande} introuvable.");

            var statutsValides = new[]
            {
                "en_attente",
                "en_cours_traitement",
                "commande",
                "traite",
                "refuse"
            };

            if (!statutsValides.Contains(statut))
                throw new ArgumentException($"Statut invalide : {statut}");

            if (statut == "refuse" && string.IsNullOrWhiteSpace(motifRefus))
                throw new ArgumentException("Le motif est obligatoire pour refuser une demande.");

            demande.Statut = statut;

            if (statut == "refuse")
                demande.MotifRefus = motifRefus!.Trim();

            await _context.SaveChangesAsync();
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();
            await _audit.LogAsync(new CreateAuditLogDto
            {
                Utilisateur = userName,
                Email       = "system",
                Action      = IAuditLogService.Actions.Changement,
                Categorie   = IAuditLogService.Categories.DemandeAchat,
                Entite      = $"Demande #{idDemande}",
                Details     = $"Statut mis à jour → {statut}" + (motifRefus != null ? $" | Motif: {motifRefus}" : "")
            });
        }

        public async Task<OffreAchat> AjouterOffreAsync(int idDemande, OffreAchat offre)
        {
            var existe = await _context.DemandeAchat
                .AnyAsync(d => d.IdDemande == idDemande);

            if (!existe)
                throw new KeyNotFoundException($"Demande ID {idDemande} introuvable.");

            offre.IdDemande  = idDemande;
            offre.EstChoisie = false;

            _context.OffreAchat.Add(offre);
            await _context.SaveChangesAsync();
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();
            await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
            {
                Type   = "demande",
                NodeId = $"d-{idDemande}"
            });

            return offre;
        }

        public async Task SupprimerOffreAsync(Guid idOffre)
        {
            var offre = await _context.OffreAchat
                .FirstOrDefaultAsync(o => o.IdOffre == idOffre);

            if (offre == null)
                throw new KeyNotFoundException($"Offre {idOffre} introuvable.");
            
            var idDemande = offre.IdDemande;
            _context.OffreAchat.Remove(offre);
            await _context.SaveChangesAsync();
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();
            await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
            {
                Type   = "demande",
                NodeId = $"d-{idDemande}"
            });
        }

        public async Task<byte[]?> GetContenuPdfAsync(Guid idOffre)
        {
            var offre = await _context.OffreAchat
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.IdOffre == idOffre);

            return offre?.ContenuPdf;
        }
        public async Task MarquerVuAsync(int idDemande)
        {
            var demande = await _context.DemandeAchat
                .FirstOrDefaultAsync(d => d.IdDemande == idDemande);
            
            if (demande != null && demande.VuParAchatLe == null)
            {
                demande.VuParAchatLe = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await _notifier.NotifyAsync();
                await _notifier.NotifyITAsync();
            }
        }

        public async Task<int> CountNonVusAsync()
        {
            return await _context.DemandeAchat
                .CountAsync(d => d.VuParAchatLe == null);
        }
    }
}
