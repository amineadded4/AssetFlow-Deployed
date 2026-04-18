using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class EmployeManagementService : IEmployeManagementService
    {
        private readonly AppDbContext _db;
        private readonly IAuditLogService _audit;
        private readonly IDashboardNotifier _notifier; // AJOUTÉ

        public EmployeManagementService(AppDbContext db, IAuditLogService audit, IDashboardNotifier notifier) // AJOUTÉ
        {
            _db       = db;
            _audit    = audit;
            _notifier = notifier; // AJOUTÉ
        }

        public async Task<List<EmployeListeDto>> GetEmployesAsync(string? search = null)
        {
            var query = _db.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(s) ||
                    u.LastName.ToLower().Contains(s)  ||
                    u.Email.ToLower().Contains(s)     ||
                    u.Role.ToLower().Contains(s));
            }

            var users = await query.OrderBy(u => u.FirstName).ToListAsync();
            var userIds = users.Select(u => u.Id).ToList();

            var counts = await _db.Affectations
                .Where(a => userIds.Contains(a.UtilisateurId.Value) && a.Etat == EtatAffectation.Courante)
                .GroupBy(a => a.UtilisateurId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();

            return users.Select(u => new EmployeListeDto
            {
                Id                    = u.Id,
                FullName              = $"{u.FirstName} {u.LastName}",
                Email                 = u.Email,
                Role                  = u.Role,
                Initials              = GetInitials(u.FirstName, u.LastName),
                NbAffectationsActives = counts.FirstOrDefault(c => c.UserId == u.Id)?.Count ?? 0,
                CreatedAt             = u.CreatedAt
            }).ToList();
        }

        public async Task<List<AffectationEmployeDto>> GetAffectationsEmployeAsync(int utilisateurId)
        {
            var affectations = await _db.Affectations
                .Include(a => a.Materiel)
                .Include(a => a.Articles)
                .Where(a => a.UtilisateurId == utilisateurId)
                .OrderByDescending(a => a.DateAffectation)
                .ToListAsync();

            return affectations.Select(a => new AffectationEmployeDto
            {
                AffectationId    = a.Id,
                MaterielId       = a.MaterielId,
                Designation      = a.Materiel.Designation,
                Reference        = a.Materiel.Reference,
                Categorie        = a.Materiel.Categorie,
                ImageUrl         = a.Materiel.ImageUrl,
                DateAffectation  = a.DateAffectation,
                DateRetourPrevue = a.DateRetour,
                Etat             = a.Etat.ToString(),
                Observations     = a.Observations,
                Articles         = a.Articles.Select(art => new ArticleAffectationDto
                {
                    ArticleId   = art.Id,
                    NumeroSerie = art.NumeroSerie ?? $"S/N #{art.Id}",
                    Etat        = art.Etat.ToString()
                }).ToList()
            }).ToList();
        }

        public async Task<RetirerAffectationResultDto> RetirerAffectationAsync(string userName, int affectationId)
        {
            var affectation = await _db.Affectations
                .Include(a => a.Articles)
                .Include(a => a.Materiel)
                .FirstOrDefaultAsync(a => a.Id == affectationId);

            if (affectation == null)
                return new RetirerAffectationResultDto { Succes = false, Message = "Affectation introuvable." };

            if (affectation.Etat == EtatAffectation.Terminee)
                return new RetirerAffectationResultDto { Succes = false, Message = "Affectation déjà terminée." };

            // Capturer les IDs AVANT modification pour les notifications
            var materielId    = affectation.MaterielId;
            var utilisateurId = affectation.UtilisateurId;
            var projetId      = affectation.ProjetId;

            // ── Résoudre les incidents non clôturés de cette affectation ────────
            var incidents = await _db.Incidents
                .Include(i => i.Article)
                .Where(i => i.AffectationId == affectationId &&
                            i.Statut != StatutIncident.Resolu &&
                            i.Statut != StatutIncident.Cloture)
                .ToListAsync();

            foreach (var inc in incidents)
            {
                inc.Statut                 = StatutIncident.Resolu;
                inc.DateResolution         = DateTime.UtcNow;
                inc.CommentairesResolution = "Résolu automatiquement lors de la révocation de l'affectation.";
            }

            // ── Remettre les articles en panne à l'état Bon + libérer ───────────
            foreach (var article in affectation.Articles)
            {
                article.Statut        = StatutArticle.Disponible;
                article.AffectationId = null;
                article.Etat          = EtatArticle.Bon;
            }

            affectation.Etat       = EtatAffectation.Terminee;
            affectation.DateRetour = DateTime.UtcNow;
            affectation.Materiel.QuantiteStock += affectation.Articles.Count;

            await _db.SaveChangesAsync();
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();

            // ── MEMORY : notifier matériel ───────────────────────────────────────
            await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
            {
                Type   = "materiel",
                NodeId = $"m-{materielId}"
            });

            // ── MEMORY : notifier utilisateur si affectation liée à un user ─────
            if (utilisateurId.HasValue)
            {
                await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
                {
                    Type   = "utilisateur",
                    NodeId = $"u-{utilisateurId.Value}"
                });
            }

            // ── MEMORY : notifier projet si affectation liée à un projet ────────
            if (projetId.HasValue)
            {
                await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
                {
                    Type   = "projet",
                    NodeId = $"p-{projetId.Value}"
                });
            }

            await _audit.LogAsync(new CreateAuditLogDto
            {
                Utilisateur = userName,
                Email       = "system",
                Action      = IAuditLogService.Actions.Revocation,
                Categorie   = IAuditLogService.Categories.Affectation,
                Entite      = $"Affectation #{affectation.Id}",
                Details     = $"{affectation.Articles.Count} article(s) de \"{affectation.Materiel.Designation}\" remis en stock" +
                            (incidents.Any() ? $", {incidents.Count} incident(s) auto-résolu(s)" : "")
            });

            return new RetirerAffectationResultDto
            {
                Succes  = true,
                Message = $"Affectation retirée. {affectation.Articles.Count} article(s) remis en stock." +
                        (incidents.Any() ? $" {incidents.Count} incident(s) auto-résolu(s)." : "")
            };
        }

        private static string GetInitials(string f, string l)
            => $"{(f.Length > 0 ? f[0].ToString() : "")}{(l.Length > 0 ? l[0].ToString() : "")}".ToUpper();
    }
}