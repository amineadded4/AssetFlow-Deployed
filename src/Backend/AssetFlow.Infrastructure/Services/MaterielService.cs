// ============================================================
// AssetFlow.Infrastructure / Services / MaterielService.cs
// MISE À JOUR : suppression en cascade (affectations + incidents)
// ============================================================

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

        // ── Helpers ───────────────────────────────────────────────
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
            Etat          = m.Etat.ToString(),
            ImageUrl      = m.ImageUrl,
            DateAjout     = m.DateAjout
        };

        private static EtatMateriel ParseEtat(string etat) =>
            Enum.TryParse<EtatMateriel>(etat, true, out var e) ? e : EtatMateriel.Disponible;

        // ── Lecture ───────────────────────────────────────────────
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

        public async Task<IEnumerable<MaterielDto>> SearchAsync(string? terme, string? categorie, string? etat)
        {
            var q = _db.Materiels.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(terme))
            {
                var t = terme.Trim().ToLower();
                q = q.Where(m =>
                    m.Designation.ToLower().Contains(t) ||
                    m.Reference.ToLower().Contains(t)   ||
                    (m.Description != null && m.Description.ToLower().Contains(t)));
            }
            if (!string.IsNullOrWhiteSpace(categorie) && categorie != "all")
                q = q.Where(m => m.Categorie.ToLower() == categorie.ToLower());
            if (!string.IsNullOrWhiteSpace(etat) && etat != "all")
            {
                var etatEnum = ParseEtat(etat);
                q = q.Where(m => m.Etat == etatEnum);
            }
            var list = await q.OrderBy(m => m.Designation).ToListAsync();
            return list.Select(ToDto);
        }

        public async Task<MaterielStatsDto> GetStatsAsync()
        {
            var all = await _db.Materiels.AsNoTracking().ToListAsync();
            return new MaterielStatsDto
            {
                TotalArticles   = all.Count,
                EnStock         = all.Count(m => m.Etat == EtatMateriel.Disponible),
                AlerteSeuil     = all.Count(m =>
                    m.Etat != EtatMateriel.EnRupture &&
                    m.QuantiteStock <= m.QuantiteMin * 2 &&
                    m.QuantiteStock > 0),
                RuptureCritique = all.Count(m => m.Etat == EtatMateriel.EnRupture || m.QuantiteStock == 0)
            };
        }

        // ── Écriture ──────────────────────────────────────────────
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
                Etat          = ParseEtat(dto.Etat),
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
            materiel.Etat          = ParseEtat(dto.Etat);
            materiel.ImageUrl      = dto.ImageUrl?.Trim();

            await _db.SaveChangesAsync();
            return new MaterielResultDto { Succes = true, Message = "Matériel mis à jour." };
        }

        /// <summary>
        /// Suppression simple (conservée pour compatibilité).
        /// Ne supprime pas si des affectations existent sans cascade.
        /// </summary>
        public async Task<MaterielResultDto> SupprimerAsync(int id)
        {
            return await SupprimerAvecCascadeAsync(id);
        }

        /// <summary>
        /// Suppression en cascade :
        ///   1. Supprime tous les incidents liés aux affectations du matériel
        ///   2. Supprime toutes les affectations du matériel
        ///   3. Supprime le matériel
        /// </summary>
        public async Task<MaterielResultDto> SupprimerAvecCascadeAsync(int id)
        {
            var materiel = await _db.Materiels.FindAsync(id);
            if (materiel is null)
                return new MaterielResultDto { Succes = false, Message = "Matériel introuvable." };

            // 1. Récupérer toutes les affectations
            var affectations = await _db.Affectations
                .Where(a => a.MaterielId == id)
                .ToListAsync();

            if (affectations.Any())
            {
                var affectationIds = affectations.Select(a => a.Id).ToList();

                // 2. Supprimer les incidents liés à ces affectations
                var incidents = await _db.Incidents
                    .Where(i => affectationIds.Contains(i.AffectationId))
                    .ToListAsync();

                if (incidents.Any())
                    _db.Incidents.RemoveRange(incidents);

                // 3. Supprimer les affectations
                _db.Affectations.RemoveRange(affectations);
            }

            // 4. Supprimer le matériel
            _db.Materiels.Remove(materiel);
            await _db.SaveChangesAsync();

            return new MaterielResultDto
            {
                Succes    = true,
                Message   = "Matériel supprimé avec ses affectations et incidents associés.",
                IdMateriel = id
            };
        }
    }
}