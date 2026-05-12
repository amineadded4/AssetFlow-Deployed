using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _db;
        private readonly IDashboardNotifier _notifier;

        public AuditLogService(AppDbContext db, IDashboardNotifier notifier)
        {
            _db = db;
            _notifier = notifier;
        }

        public async Task LogAsync(CreateAuditLogDto dto)
        {
            var log = new AuditLog
            {
                Timestamp   = DateTime.UtcNow,
                Utilisateur = dto.Utilisateur,
                Email       = dto.Email,
                Action      = dto.Action,
                Categorie   = dto.Categorie,
                Entite      = dto.Entite,
                Details     = dto.Details,
                UserId      = dto.UserId
            };
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();
            
        }

        public async Task<AuditLogPagedDto> GetLogsAsync(AuditLogQueryDto query)
        {
            var q = _db.AuditLogs.AsNoTracking().AsQueryable();

            // ── Filtres ──
            if (query.DateDebut.HasValue)
            {
                var d = DateTime.SpecifyKind(query.DateDebut.Value, DateTimeKind.Utc);
                q = q.Where(l => l.Timestamp >= d);
            }
            if (query.DateFin.HasValue)
            {
                var d = DateTime.SpecifyKind(query.DateFin.Value.AddDays(1), DateTimeKind.Utc);
                q = q.Where(l => l.Timestamp <= d);
            }

            if (!string.IsNullOrWhiteSpace(query.Utilisateur) && query.Utilisateur != "Tous les utilisateurs")
                q = q.Where(l => l.Email == query.Utilisateur || l.Utilisateur.Contains(query.Utilisateur));

            if (!string.IsNullOrWhiteSpace(query.Action) && query.Action != "Toutes les actions")
                q = q.Where(l => l.Action == query.Action);

            if (!string.IsNullOrWhiteSpace(query.Categorie) && query.Categorie != "Toutes")
                q = q.Where(l => l.Categorie == query.Categorie);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(l =>
                    l.Entite.ToLower().Contains(s) ||
                    l.Utilisateur.ToLower().Contains(s) ||
                    l.Email.ToLower().Contains(s) ||
                    (l.Details != null && l.Details.ToLower().Contains(s)));
            }

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(l => l.Timestamp)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(l => new AuditLogDto
                {
                    Id          = l.Id,
                    Timestamp   = l.Timestamp,
                    Utilisateur = l.Utilisateur,
                    Email       = l.Email,
                    Action      = l.Action,
                    Categorie   = l.Categorie,
                    Entite      = l.Entite,
                    Details     = l.Details
                })
                .ToListAsync();

            return new AuditLogPagedDto
            {
                Items      = items,
                Total      = total,
                Page       = query.Page,
                PageSize   = query.PageSize,
                TotalPages = (int)Math.Ceiling(total / (double)query.PageSize)
            };
        }

        public async Task<int> SupprimerAvantDateAsync(DateTime date)
        {
            var utcDate = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            var logs = await _db.AuditLogs
                .Where(l => l.Timestamp < utcDate)
                .ToListAsync();

            _db.AuditLogs.RemoveRange(logs);
            await _db.SaveChangesAsync();
            return logs.Count;
        }

        public async Task<int> SupprimerParCategorieAsync(string categorie)
        {
            // Protection — refuser si catégorie vide
            if (string.IsNullOrWhiteSpace(categorie))
                return 0;

            Console.WriteLine($"[AUDIT] Suppression catégorie : '{categorie}'");

            var logs = await _db.AuditLogs
                .Where(l => l.Categorie == categorie)
                .ToListAsync();

            Console.WriteLine($"[AUDIT] Entrées trouvées : {logs.Count}");

            if (logs.Count == 0) return 0;

            _db.AuditLogs.RemoveRange(logs);
            await _db.SaveChangesAsync();
            return logs.Count;
        }

        public async Task<int> SupprimerToutAsync()
        {
            var total = await _db.AuditLogs.CountAsync();
            await _db.AuditLogs.ExecuteDeleteAsync(); // EF Core 7+
            return total;
        }

        public async Task<AuditLogStatsDto> GetStatsAsync()
        {
            var now   = DateTime.UtcNow;
            var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
            var month = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc);

            var parCategorie = await _db.AuditLogs
                .GroupBy(l => l.Categorie)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            return new AuditLogStatsDto
            {
                TotalEntrees        = await _db.AuditLogs.CountAsync(),
                EntreesAujourdhui   = await _db.AuditLogs.CountAsync(l => l.Timestamp >= today),
                EntreesCeMois       = await _db.AuditLogs.CountAsync(l => l.Timestamp >= month),
                PlusAncienneEntree  = await _db.AuditLogs.MinAsync(l => (DateTime?)l.Timestamp),
                ParCategorie        = parCategorie
            };
        }
    }
}