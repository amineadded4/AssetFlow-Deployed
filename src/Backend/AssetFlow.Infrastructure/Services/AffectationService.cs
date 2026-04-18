using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class AffectationService : IAffectationService
    {
        private readonly AppDbContext _db;
        private readonly IDashboardNotifier _notifier;
        private readonly IAuditLogService _audit;

        public AffectationService(AppDbContext db, IDashboardNotifier notifier,IAuditLogService a)
        { _db = db; _notifier = notifier; _audit = a;}

        // ── Utilisateurs ─────────────────────────────────────
        public async Task<List<UtilisateurDisponibleDto>> GetUtilisateursDisponiblesAsync(string? search = null)
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

            var users = await query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();

            return users.Select(u => new UtilisateurDisponibleDto
            {
                Id         = u.Id,
                FullName   = $"{u.FirstName} {u.LastName}",
                Email      = u.Email,
                Role = u.Role,
                Initials   = GetInitials(u.FirstName, u.LastName)
            }).ToList();
        }

        // ── Matériels ────────────────────────────────────────
        public async Task<List<MaterielDisponibleDto>> GetMaterielsDisponiblesAsync(string? search = null)
        {
            var query = _db.Materiels.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(m =>
                    m.Designation.ToLower().Contains(s) ||
                    m.Reference.ToLower().Contains(s)   ||
                    m.Categorie.ToLower().Contains(s));
            }

            var materiels = await query.OrderBy(m => m.Designation).ToListAsync();
            var materielIds = materiels.Select(m => m.Id).ToList();

            var articles = await _db.ArticlesIndividuels
                .AsNoTracking()
                .Where(a => materielIds.Contains(a.MaterielId) && a.Statut == StatutArticle.Disponible)
                .ToListAsync();

            var result = new List<MaterielDisponibleDto>();
            foreach (var m in materiels)
            {
                var arts = articles.Where(a => a.MaterielId == m.Id).ToList();
                if (arts.Count == 0) continue;

                result.Add(new MaterielDisponibleDto
                {
                    Id                 = m.Id,
                    Reference          = m.Reference,
                    Designation        = m.Designation,
                    Categorie          = m.Categorie,
                    ImageUrl           = m.ImageUrl,
                    QuantiteDisponible = arts.Count,
                    Articles           = arts.Select(a => new ArticleDisponibleDto
                    {
                        Id          = a.Id,
                        NumeroSerie = a.NumeroSerie ?? $"S/N #{a.Id}",
                        Etat        = a.Etat.ToString()
                    }).ToList()
                });
            }

            return result;
        }

        // ── Projets ────────────────────────────────
        public async Task<List<ProjetDisponibleDto>> GetProjetsDisponiblesAsync(string? search = null)
        {
            var query = _db.Projects.AsNoTracking()
                .Where(p => p.Statut != StatutProjet.Termine);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Nom.ToLower().Contains(s) ||
                    (p.Responsable != null && p.Responsable.ToLower().Contains(s)));
            }

            var projets = await query.OrderBy(p => p.Nom).ToListAsync();

            return projets.Select(p => new ProjetDisponibleDto
            {
                Id          = p.Id,
                Nom         = p.Nom,
                Statut      = p.Statut.ToString(),
                Priorite    = p.Priorite.ToString(),
                Responsable = p.Responsable
            }).ToList();
        }

        // ── Créer affectation ────────────────────────────────
        public async Task<AffectationResultDto> CreerAffectationAsync(CreerAffectationDto dto)
        {
            if (dto.ArticleIds == null || dto.ArticleIds.Count == 0)
                return new AffectationResultDto { Succes = false, Message = "Aucun article sélectionné." };

            User? utilisateur = null;
            if (dto.UtilisateurId.HasValue)
            {
                utilisateur = await _db.Users.FindAsync(dto.UtilisateurId.Value);
                if (utilisateur == null)
                    return new AffectationResultDto { Succes = false, Message = "Utilisateur introuvable." };
            }
            if (!dto.ProjetId.HasValue && utilisateur == null)
                return new AffectationResultDto { Succes = false, Message = "Utilisateur ou projet requis." };

            var materiel = await _db.Materiels.FindAsync(dto.MaterielId);
            if (materiel == null)
                return new AffectationResultDto { Succes = false, Message = "Matériel introuvable." };

            // Vérifier projet si fourni
            if (dto.ProjetId.HasValue)
            {
                var projet = await _db.Projects.FindAsync(dto.ProjetId.Value);
                if (projet == null)
                    return new AffectationResultDto { Succes = false, Message = "Projet introuvable." };
            }

            var articles = await _db.ArticlesIndividuels
                .Where(a => dto.ArticleIds.Contains(a.Id))
                .ToListAsync();

            if (articles.Count != dto.ArticleIds.Count)
                return new AffectationResultDto { Succes = false, Message = "Certains articles sont introuvables." };

            var nonDispo = articles.Where(a => a.Statut != StatutArticle.Disponible).ToList();
            if (nonDispo.Any())
                return new AffectationResultDto
                {
                    Succes  = false,
                    Message = $"{nonDispo.Count} article(s) ne sont plus disponibles."
                };

            var affectation = new Affectation
            {
                MaterielId       = dto.MaterielId,
                UtilisateurId    = dto.UtilisateurId,
                ProjetId         = dto.ProjetId,
                DateAffectation  = DateTime.UtcNow,
                QuantiteAffectee = articles.Count,
                Observations     = dto.Observations?.Trim(),
                DateRetour       = dto.DateRetourPrevue
            };

            _db.Affectations.Add(affectation);
            await _db.SaveChangesAsync();

            foreach (var article in articles)
            {
                article.Statut        = StatutArticle.Affecte;
                article.AffectationId = affectation.Id;
            }

            materiel.QuantiteStock = Math.Max(0, materiel.QuantiteStock - articles.Count);
            await _db.SaveChangesAsync();
            await _db.SaveChangesAsync();
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();

            // ── MEMORY : matériel toujours notifié ──────────────────────────────
            await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
            {
                Type   = "materiel",
                NodeId = $"m-{dto.MaterielId}"
            });

            // ── MEMORY : utilisateur si affectation à un user ───────────────────
            if (dto.UtilisateurId.HasValue)
            {
                await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
                {
                    Type   = "utilisateur",
                    NodeId = $"u-{dto.UtilisateurId.Value}"
                });
            }

            // ── MEMORY : projet si affectation à un projet ──────────────────────
            if (dto.ProjetId.HasValue)
            {
                await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
                {
                    Type   = "projet",
                    NodeId = $"p-{dto.ProjetId.Value}"
                });
            }

            var beneficiaire = dto.ProjetId.HasValue
                ? (await _db.Projects.FindAsync(dto.ProjetId.Value))?.Nom ?? "projet"
                : $"{utilisateur.FirstName} {utilisateur.LastName}";
            await _audit.LogAsync(new CreateAuditLogDto
            {
                Utilisateur = dto.user_name,
                Email       = "system",
                Action      = IAuditLogService.Actions.Affectation,
                Categorie   = IAuditLogService.Categories.Affectation,
                Entite      = $"Affectation #{affectation.Id}",
                Details     = $"{articles.Count} article(s) de \"{materiel.Designation}\" affecté(s) à {beneficiaire}"
            });

            return new AffectationResultDto
            {
                Succes        = true,
                Message       = $"Affectation créée avec succès pour {beneficiaire}.",
                AffectationId = affectation.Id
            };
        }

        private static string GetInitials(string firstName, string lastName)
        {
            var f = string.IsNullOrEmpty(firstName) ? "" : firstName[0].ToString().ToUpper();
            var l = string.IsNullOrEmpty(lastName)  ? "" : lastName[0].ToString().ToUpper();
            return f + l;
        }
    }
}