using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class MaterielService : IMaterielService
    {
        private readonly AppDbContext _db;
        private readonly IDashboardNotifier _notifier;
        private readonly IAuditLogService _audit;
        private readonly CloudinaryService _cloudinary;

        public MaterielService(AppDbContext db, IDashboardNotifier notifier, IAuditLogService audit,CloudinaryService cloudinary)
        { _db = db; _notifier = notifier; _audit = audit; _cloudinary = cloudinary; }

        private static MaterielDto ToDto(Materiel m) => new()
        {
            Id            = m.Id,
            Reference     = m.Reference,
            Designation   = m.Designation,
            Description   = m.Description,
            Categorie     = m.Categorie,
            QuantiteStock = m.QuantiteStock,
            QuantiteMin   = m.QuantiteMin,
            Unite         = m.Unite,
            Emplacement   = m.Emplacement,
            ImageUrl      = m.ImageUrl,
            DateAjout     = m.DateAjout
        };

        public async Task<IEnumerable<MaterielDto>> GetAllAsync()
        {
            var list = await _db.Materiels.AsNoTracking().OrderByDescending(m => m.DateAjout).ToListAsync();
            return list.Select(ToDto);
        }

        public async Task<MaterielDto?> GetByIdAsync(int id)
        {
            var m = await _db.Materiels.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return m is null ? null : ToDto(m);
        }

        public async Task<IEnumerable<MaterielDto>> SearchAsync(string? terme, string? categorie)
        {
            var q = _db.Materiels.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(terme))
            {
                var t = terme.Trim().ToLower();
                q = q.Where(m =>
                    m.Designation.ToLower().Contains(t) ||
                    m.Reference.ToLower().Contains(t) ||
                    (m.Description != null && m.Description.ToLower().Contains(t)));
            }
            if (!string.IsNullOrWhiteSpace(categorie) && categorie != "all")
                q = q.Where(m => m.Categorie.ToLower() == categorie.ToLower());
            var list = await q.OrderBy(m => m.Designation).ToListAsync();
            return list.Select(ToDto);
        }

        public async Task<MaterielStatsDto> GetStatsAsync()
        {
            var all = await _db.Materiels.AsNoTracking().ToListAsync();
            return new MaterielStatsDto
            {
                TotalArticles   = all.Count,
                EnStock         = all.Count(m => m.QuantiteStock > m.QuantiteMin),
                AlerteSeuil     = all.Count(m => m.QuantiteStock <= m.QuantiteMin && m.QuantiteStock > 0),
                RuptureCritique = all.Count(m => m.QuantiteStock == 0)
            };
        }

        public async Task<MaterielResultDto> CreerAsync(CreerMaterielDto dto)
        {
            if (await _db.Materiels.AnyAsync(m => m.Reference == dto.Reference.Trim()))
                return new MaterielResultDto { Succes = false, Message = "Cette référence existe déjà." };
            string? imageUrl = null;
            if (!string.IsNullOrEmpty(dto.ImageUrl) && dto.ImageUrl.StartsWith("data:"))
            {
                var publicId = $"{dto.Reference.Trim()}_{DateTime.UtcNow.Ticks}";
                imageUrl = await _cloudinary.UploadBase64Async(dto.ImageUrl, publicId);
            }

            var materiel = new Materiel
            {
                Reference     = dto.Reference.Trim(),
                Designation   = dto.Designation.Trim(),
                Description   = dto.Description?.Trim(),
                Categorie     = dto.Categorie.Trim(),
                QuantiteStock = dto.QuantiteStock,
                QuantiteMin   = dto.QuantiteMin,
                Unite         = dto.Unite.Trim(),
                Emplacement   = dto.Emplacement?.Trim(),
                ImageUrl      = imageUrl,
                DateAjout     = DateTime.UtcNow  // ✅ déjà UTC, aucun changement nécessaire
            };

            _db.Materiels.Add(materiel);
            await _db.SaveChangesAsync();
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();
            await _audit.LogAsync(new CreateAuditLogDto
            {
                Utilisateur = dto.Utilisateur,
                Email       = "system",
                Action      = IAuditLogService.Actions.Creation,
                Categorie   = IAuditLogService.Categories.Materiel,
                Entite      = $"Matériel #{materiel.Reference}",
                Details     = $"Nouveau matériel ajouté : \"{materiel.Designation}\" (Qté: {materiel.QuantiteStock})"
            });

            return new MaterielResultDto { Succes = true, Message = "Matériel créé avec succès.", IdMateriel = materiel.Id };
        }

        public async Task<MaterielResultDto> ModifierAsync(ModifierMaterielDto dto)
        {
            var materiel = await _db.Materiels.FindAsync(dto.Id);
            if (materiel is null)
                return new MaterielResultDto { Succes = false, Message = "Matériel introuvable." };

            if (await _db.Materiels.AnyAsync(m => m.Reference == dto.Reference.Trim() && m.Id != dto.Id))
                return new MaterielResultDto { Succes = false, Message = "Cette référence est déjà utilisée." };

            // ── Gestion image Cloudinary ─────────────────────────────
            if (dto.ImageUrl == null)
            {
                // Image supprimée → supprimer de Cloudinary
                await _cloudinary.DeleteByUrlAsync(materiel.ImageUrl);
                materiel.ImageUrl = null;
            }
            else if (dto.ImageUrl.StartsWith("data:"))
            {
                // Nouvelle image base64 → supprimer ancienne + uploader nouvelle
                await _cloudinary.DeleteByUrlAsync(materiel.ImageUrl);
                var publicId = $"{dto.Reference.Trim()}_{DateTime.UtcNow.Ticks}";
                materiel.ImageUrl = await _cloudinary.UploadBase64Async(dto.ImageUrl, publicId);
            }
            else if (dto.ImageUrl.StartsWith("https://"))
            {
                // URL Cloudinary existante → image inchangée
                materiel.ImageUrl = dto.ImageUrl;
            }
            // ────────────────────────────────────────────────────────

            materiel.Reference     = dto.Reference.Trim();
            materiel.Designation   = dto.Designation.Trim();
            materiel.Description   = dto.Description?.Trim();
            materiel.Categorie     = dto.Categorie.Trim();
            materiel.QuantiteStock = dto.QuantiteStock;
            materiel.QuantiteMin   = dto.QuantiteMin;
            materiel.Unite         = dto.Unite.Trim();
            materiel.Emplacement   = dto.Emplacement?.Trim();

            await _db.SaveChangesAsync();
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();
            await _audit.LogAsync(new CreateAuditLogDto
            {
                Utilisateur = dto.Utilisateur,
                Email       = "system",
                Action      = IAuditLogService.Actions.Modification,
                Categorie   = IAuditLogService.Categories.Materiel,
                Entite      = $"Matériel #{dto.Reference}",
                Details     = $"Matériel mis à jour : \"{dto.Designation}\""
            });

            return new MaterielResultDto { Succes = true, Message = "Matériel mis à jour." };
        }

        public async Task<MaterielResultDto> SupprimerAsync(string userName, int id)
            => await SupprimerAvecCascadeAsync(userName, id);

        public async Task<MaterielResultDto> SupprimerAvecCascadeAsync(string userName, int id)
        {
            var materiel = await _db.Materiels.FindAsync(id);
            if (materiel is null)
                return new MaterielResultDto { Succes = false, Message = "Matériel introuvable." };

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var articles    = await _db.ArticlesIndividuels.Where(a => a.MaterielId == id).ToListAsync();
                var articleIds  = articles.Select(a => a.Id).ToList();

                if (articleIds.Any())
                {
                    var incidentsArticles = await _db.Incidents
                        .Where(i => i.ArticleId.HasValue && articleIds.Contains(i.ArticleId.Value))
                        .ToListAsync();
                    if (incidentsArticles.Any()) _db.Incidents.RemoveRange(incidentsArticles);
                }

                var affectations    = await _db.Affectations.Where(a => a.MaterielId == id).ToListAsync();
                var affectationIds  = affectations.Select(a => a.Id).ToList();

                if (affectationIds.Any())
                {
                    var incidentsAffect = await _db.Incidents
                        .Where(i => affectationIds.Contains(i.AffectationId))
                        .ToListAsync();
                    if (incidentsAffect.Any()) _db.Incidents.RemoveRange(incidentsAffect);

                    _db.Affectations.RemoveRange(affectations);
                }

                if (articles.Any()) _db.ArticlesIndividuels.RemoveRange(articles);

                var commandes = await _db.Commandes.Where(c => c.MaterielId == id).ToListAsync();
                if (commandes.Any()) _db.Commandes.RemoveRange(commandes);

                _db.Materiels.Remove(materiel);

                await _db.SaveChangesAsync();
                await _notifier.NotifyAsync();
                await _notifier.NotifyITAsync();
                await tx.CommitAsync();

                await _audit.LogAsync(new CreateAuditLogDto
                {
                    Utilisateur = userName,
                    Email       = "system",
                    Action      = IAuditLogService.Actions.Suppression,
                    Categorie   = IAuditLogService.Categories.Materiel,
                    Entite      = $"Matériel #{materiel.Reference}",
                    Details     = $"Supprimé avec cascade : \"{materiel.Designation}\""
                });

                return new MaterielResultDto
                {
                    Succes     = true,
                    Message    = "Matériel supprimé avec toutes ses données associées.",
                    IdMateriel = id
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new MaterielResultDto
                {
                    Succes  = false,
                    Message = $"Erreur : {ex.InnerException?.Message ?? ex.Message}"
                };
            }
        }
        public async Task<IEnumerable<MaterielAlerteDto>> GetAlertesStockAsync()
        {
            var list = await _db.Materiels
                .AsNoTracking()
                .Where(m => m.QuantiteStock <= m.QuantiteMin)
                .OrderBy(m => m.QuantiteStock)
                .ToListAsync();

            return list.Select(m => new MaterielAlerteDto
            {
                Reference     = m.Reference,
                Designation   = m.Designation,
                Categorie     = m.Categorie,
                QuantiteStock = m.QuantiteStock,
                QuantiteMin   = m.QuantiteMin,
                Emplacement   = m.Emplacement ?? "—",
                Niveau        = m.QuantiteStock == 0 ? "🔴 Critique" : "🟡 Alerte"
            });
        }
    }
}