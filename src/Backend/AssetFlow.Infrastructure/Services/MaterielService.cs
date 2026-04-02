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
        public MaterielService(AppDbContext db) => _db = db;

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
            var list = await _db.Materiels.AsNoTracking().OrderBy(m => m.Designation).ToListAsync();
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
                ImageUrl      = dto.ImageUrl?.Trim(),
                DateAjout     = DateTime.UtcNow
            };

            _db.Materiels.Add(materiel);
            await _db.SaveChangesAsync();
            return new MaterielResultDto { Succes = true, Message = "Matériel créé avec succès.", IdMateriel = materiel.Id };
        }

        public async Task<MaterielResultDto> ModifierAsync(ModifierMaterielDto dto)
        {
            var materiel = await _db.Materiels.FindAsync(dto.Id);
            if (materiel is null)
                return new MaterielResultDto { Succes = false, Message = "Matériel introuvable." };

            if (await _db.Materiels.AnyAsync(m => m.Reference == dto.Reference.Trim() && m.Id != dto.Id))
                return new MaterielResultDto { Succes = false, Message = "Cette référence est déjà utilisée." };

            materiel.Reference     = dto.Reference.Trim();
            materiel.Designation   = dto.Designation.Trim();
            materiel.Description   = dto.Description?.Trim();
            materiel.Categorie     = dto.Categorie.Trim();
            materiel.QuantiteStock = dto.QuantiteStock;
            materiel.QuantiteMin   = dto.QuantiteMin;
            materiel.Unite         = dto.Unite.Trim();
            materiel.Emplacement   = dto.Emplacement?.Trim();
            materiel.ImageUrl      = dto.ImageUrl?.Trim();

            await _db.SaveChangesAsync();
            return new MaterielResultDto { Succes = true, Message = "Matériel mis à jour." };
        }

        public async Task<MaterielResultDto> SupprimerAsync(int id)
            => await SupprimerAvecCascadeAsync(id);

        //   1. Incidents liés aux articles de toutes les commandes
        //   2. Articles individuels de toutes les commandes
        //   3. Commandes du matériel
        //   4. Incidents liés aux affectations (via AffectationId)
        //   5. Affectations du matériel
        //   6. Le matériel lui-même
        public async Task<MaterielResultDto> SupprimerAvecCascadeAsync(int id)
        {
            var materiel = await _db.Materiels.FindAsync(id);
            if (materiel is null)
                return new MaterielResultDto { Succes = false, Message = "Matériel introuvable." };

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. Articles de toutes les commandes de ce matériel
                var articles = await _db.ArticlesIndividuels
                    .Where(a => a.MaterielId == id)
                    .ToListAsync();

                var articleIds = articles.Select(a => a.Id).ToList();

                // 2. Incidents liés aux articles
                if (articleIds.Any())
                {
                    var incidentsArticles = await _db.Incidents
                        .Where(i => i.ArticleId.HasValue && articleIds.Contains(i.ArticleId.Value))
                        .ToListAsync();
                    if (incidentsArticles.Any()) _db.Incidents.RemoveRange(incidentsArticles);
                }

                // 3. Affectations du matériel
                var affectations = await _db.Affectations
                    .Where(a => a.MaterielId == id)
                    .ToListAsync();

                var affectationIds = affectations.Select(a => a.Id).ToList();

                // 4. Incidents liés aux affectations
                if (affectationIds.Any())
                {
                    var incidentsAffect = await _db.Incidents
                        .Where(i => affectationIds.Contains(i.AffectationId))
                        .ToListAsync();
                    if (incidentsAffect.Any()) _db.Incidents.RemoveRange(incidentsAffect);

                    _db.Affectations.RemoveRange(affectations);
                }

                // 5. Articles individuels
                if (articles.Any()) _db.ArticlesIndividuels.RemoveRange(articles);

                // 6. Commandes
                var commandes = await _db.Commandes
                    .Where(c => c.MaterielId == id)
                    .ToListAsync();
                if (commandes.Any()) _db.Commandes.RemoveRange(commandes);

                // 7. Matériel
                _db.Materiels.Remove(materiel);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

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
                return new MaterielResultDto { Succes = false, Message = $"Erreur : {ex.InnerException?.Message ?? ex.Message}" };
            }
        }
    }
}