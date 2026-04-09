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
        public EmployeManagementService(AppDbContext db, IAuditLogService audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<List<EmployeListeDto>> GetEmployesAsync(string? search = null)
        {
            var query = _db.Users.AsNoTracking()
                .Where(u => u.Role != "Admin");

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

            // Compter les affectations Courantes par employé
            var counts = await _db.Affectations
                .Where(a => userIds.Contains(a.UtilisateurId.Value) && a.Etat == EtatAffectation.Courante)
                .GroupBy(a => a.UtilisateurId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();

            return users.Select(u => new EmployeListeDto
            {
                Id         = u.Id,
                FullName   = $"{u.FirstName} {u.LastName}",
                Email      = u.Email,
                Role = u.Role,
                Initials   = GetInitials(u.FirstName, u.LastName),
                NbAffectationsActives = counts.FirstOrDefault(c => c.UserId == u.Id)?.Count ?? 0,
                CreatedAt  = u.CreatedAt
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
                AffectationId     = a.Id,
                MaterielId        = a.MaterielId,
                Designation       = a.Materiel.Designation,
                Reference         = a.Materiel.Reference,
                Categorie         = a.Materiel.Categorie,
                ImageUrl          = a.Materiel.ImageUrl,
                DateAffectation   = a.DateAffectation,
                DateRetourPrevue  = a.DateRetour,
                Etat              = a.Etat.ToString(),
                Observations      = a.Observations,
                Articles          = a.Articles.Select(art => new ArticleAffectationDto
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

            // Remettre les articles en Disponible
            foreach (var article in affectation.Articles)
            {
                article.Statut        = StatutArticle.Disponible;
                article.AffectationId = null;
            }

            // Marquer l'affectation comme Terminée
            affectation.Etat        = EtatAffectation.Terminee;
            affectation.DateRetour  = DateTime.UtcNow;

            // Remettre le stock
            affectation.Materiel.QuantiteStock += affectation.Articles.Count;

            await _db.SaveChangesAsync();
            await _audit.LogAsync(new CreateAuditLogDto
            {
                Utilisateur = userName,
                Email       = "system",
                Action      = IAuditLogService.Actions.Revocation,
                Categorie   = IAuditLogService.Categories.Affectation,
                Entite      = $"Affectation #{affectation.Id}",
                Details     = $"{affectation.Articles.Count} article(s) de \"{affectation.Materiel.Designation}\" remis en stock"
            });

            return new RetirerAffectationResultDto
            {
                Succes  = true,
                Message = $"Affectation retirée. {affectation.Articles.Count} article(s) remis en stock."
            };
        }

        private static string GetInitials(string f, string l)
            => $"{(f.Length > 0 ? f[0].ToString() : "")}{(l.Length > 0 ? l[0].ToString() : "")}".ToUpper();
    }
}