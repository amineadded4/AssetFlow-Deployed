using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class ProjectService : IProjectService
    {
        private readonly AppDbContext _db;
        private readonly IDashboardNotifier _notifier; // AJOUTÉ

        public ProjectService(AppDbContext db, IDashboardNotifier notifier) // AJOUTÉ
        {
            _db       = db;
            _notifier = notifier;
        }

        public async Task<List<ProjectDto>> GetAllAsync()
            => await _db.Projects.OrderByDescending(p => p.CreatedAt).Select(p => ToDto(p)).ToListAsync();

        public async Task<ProjectDto?> GetByIdAsync(int id)
        {
            var p = await _db.Projects.FindAsync(id);
            return p == null ? null : ToDto(p);
        }

        public async Task<ProjectDto> CreateAsync(CreateProjectDto dto)
        {
            var p = new Project
            {
                Nom         = dto.Nom,
                Description = dto.Description,
                Statut      = Enum.Parse<StatutProjet>(dto.Statut),
                Priorite    = Enum.Parse<PrioriteProjet>(dto.Priorite),
                Responsable = dto.Responsable,
                Budget      = dto.Budget,
                DateDebut   = dto.DateDebut,
                DateFin     = dto.DateFin,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            };
            _db.Projects.Add(p);
            await _db.SaveChangesAsync();
            // ── MEMORY ──────────────────────────────────────────────────────────
            await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
            {
                Type   = "projet",
                NodeId = $"p-{p.Id}"
            });
            // ────────────────────────────────────────────────────────────────────
            return ToDto(p);
        }

        public async Task<ProjectDto?> UpdateAsync(int id, UpdateProjectDto dto)
        {
            var p = await _db.Projects.FindAsync(id);
            if (p == null) return null;

            p.Nom         = dto.Nom;
            p.Description = dto.Description;
            p.Statut      = Enum.Parse<StatutProjet>(dto.Statut);
            p.Priorite    = Enum.Parse<PrioriteProjet>(dto.Priorite);
            p.Responsable = dto.Responsable;
            p.Budget      = dto.Budget;
            p.DateDebut   = dto.DateDebut;
            p.DateFin     = dto.DateFin;
            p.UpdatedAt   = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            // ── MEMORY ──────────────────────────────────────────────────────────
            await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
            {
                Type   = "projet",
                NodeId = $"p-{id}"
            });
            // ────────────────────────────────────────────────────────────────────
            return ToDto(p);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var p = await _db.Projects.FindAsync(id);
            if (p == null) return false;
            _db.Projects.Remove(p);
            await _db.SaveChangesAsync();
            // ── MEMORY ──────────────────────────────────────────────────────────
            await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
            {
                Type   = "projet",
                NodeId = $"p-{id}"
            });
            // ────────────────────────────────────────────────────────────────────
            return true;
        }

        public async Task<List<AffectationProjetDto>> GetAffectationsAsync(int projetId)
            => await _db.Affectations
                .AsNoTracking()
                .Include(a => a.Materiel)
                .Where(a => a.ProjetId == projetId)
                .OrderByDescending(a => a.DateAffectation)
                .Select(a => new AffectationProjetDto
                {
                    AffectationId    = a.Id,
                    Designation      = a.Materiel.Designation,
                    Reference        = a.Materiel.Reference,
                    QuantiteAffectee = a.QuantiteAffectee,
                    DateAffectation  = a.DateAffectation,
                    DateRetourPrevue = a.DateRetour,
                    Etat             = a.Etat.ToString(),
                    ImageUrl = a.Materiel.ImageUrl
                })
                .ToListAsync();

        private static ProjectDto ToDto(Project p) => new()
        {
            Id          = p.Id,
            Nom         = p.Nom,
            Description = p.Description,
            Statut      = p.Statut.ToString(),
            Priorite    = p.Priorite.ToString(),
            Responsable = p.Responsable,
            Budget      = p.Budget,
            DateDebut   = p.DateDebut,
            DateFin     = p.DateFin,
            CreatedAt   = p.CreatedAt,
            UpdatedAt   = p.UpdatedAt
        };
    }
}