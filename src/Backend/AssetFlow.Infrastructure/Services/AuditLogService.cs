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

        public AuditLogService(AppDbContext db) => _db = db;

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
        }

        public async Task<AuditLogPagedDto> GetLogsAsync(AuditLogQueryDto query)
        {
            var q = _db.AuditLogs.AsNoTracking().AsQueryable();

            // ── Filtres ──
            if (query.DateDebut.HasValue)
                q = q.Where(l => l.Timestamp >= query.DateDebut.Value);

            if (query.DateFin.HasValue)
                q = q.Where(l => l.Timestamp <= query.DateFin.Value.AddDays(1));

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
    }
}