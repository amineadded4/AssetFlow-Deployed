using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext      _db;
        private readonly IDashboardNotifier _notifier;
        private readonly GeoIpService      _geoIp;

        public AuditLogService(AppDbContext db, IDashboardNotifier notifier, GeoIpService geoIp)
        {
            _db       = db;
            _notifier = notifier;
            _geoIp    = geoIp;
        }

        public async Task LogAsync(CreateAuditLogDto dto)
        {
            // Géolocalisation si IP fournie
            string? geoLocation = null;
            if (!string.IsNullOrEmpty(dto.IpAddress))
                geoLocation = await _geoIp.GetLocationAsync(dto.IpAddress);

            var log = new AuditLog
            {
                Timestamp   = DateTime.UtcNow,
                Utilisateur = dto.Utilisateur,
                Email       = dto.Email,
                Action      = dto.Action,
                Categorie   = dto.Categorie,
                Entite      = dto.Entite,
                Details     = dto.Details,
                UserId      = dto.UserId,
                IpAddress   = dto.IpAddress,
                GeoLocation = geoLocation ?? dto.GeoLocation,
            };
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();
        }

        public async Task<AuditLogPagedDto> GetLogsAsync(AuditLogQueryDto query)
        {
            var q = _db.AuditLogs.AsNoTracking().AsQueryable();

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
                    Details     = l.Details,
                    IpAddress   = l.IpAddress,   // NOUVEAU
                    GeoLocation = l.GeoLocation, // NOUVEAU
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