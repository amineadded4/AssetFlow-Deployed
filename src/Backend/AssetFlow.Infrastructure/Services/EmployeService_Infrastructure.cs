// ============================================================
// AssetFlow.Infrastructure / Services / EmployeService.cs  (FICHIER COMPLET)
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class EmployeService : IEmployeService
    {
        private readonly AppDbContext _context;

        public EmployeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<EquipementAffecteDto>> GetEquipementsAffectesAsync(int utilisateurId)
        {
            var affectations = await _context.Affectations
                .Include(a => a.Materiel)
                .Where(a => a.UtilisateurId == utilisateurId)
                .OrderByDescending(a => a.DateAffectation)
                .ToListAsync();

            return affectations.Select(a => new EquipementAffecteDto
            {
                AffectationId    = a.Id,
                MaterielId       = a.MaterielId,
                Reference        = a.Materiel.Reference,
                Designation      = a.Materiel.Designation,
                Categorie        = a.Materiel.Categorie,
                ImageUrl         = a.Materiel.ImageUrl,
                DateAffectation  = a.DateAffectation,
                QuantiteAffectee = a.QuantiteAffectee,
                Observations     = a.Observations
            }).ToList();
        }

        public async Task<EquipementAffecteDto?> GetEquipementDetailAsync(int affectationId, int articleId = 0)
        {
            var affectation = await _context.Affectations
                .Include(a => a.Materiel)
                .Include(a => a.Articles)
                .FirstOrDefaultAsync(a => a.Id == affectationId);

            if (affectation == null) return null;

            var article = articleId > 0
                ? affectation.Articles.FirstOrDefault(a => a.Id == articleId)
                  ?? affectation.Articles.FirstOrDefault()
                : affectation.Articles.FirstOrDefault();

            return new EquipementAffecteDto
            {
                AffectationId    = affectation.Id,
                MaterielId       = affectation.MaterielId,
                Reference        = affectation.Materiel.Reference,
                Designation      = affectation.Materiel.Designation,
                Categorie        = affectation.Materiel.Categorie,
                ImageUrl         = affectation.Materiel.ImageUrl,
                DateAffectation  = affectation.DateAffectation,
                QuantiteAffectee = affectation.QuantiteAffectee,
                Observations     = affectation.Observations,
                NumeroSerie      = article?.NumeroSerie,
                EtatArticle      = article?.Etat.ToString() ?? "Bon"
            };
        }

        public async Task<List<MaterielAffecteGroupeDto>> GetMaterielsGroupesAsync(int utilisateurId)
        {
            var affectations = await _context.Affectations
                .Include(a => a.Materiel)
                .Where(a => a.UtilisateurId == utilisateurId && a.Etat == EtatAffectation.Courante)
                .OrderByDescending(a => a.DateAffectation)
                .ToListAsync();

            if (!affectations.Any())
                return new List<MaterielAffecteGroupeDto>();

            var affectationIds = affectations.Select(a => a.Id).ToList();

            var articles = await _context.ArticlesIndividuels
                .Where(art => art.AffectationId.HasValue
                           && affectationIds.Contains(art.AffectationId.Value))
                .ToListAsync();

            // Récupérer les compteurs de commentaires pour tous les matériels concernés
            var materielIds = affectations.Select(a => a.MaterielId).Distinct().ToList();
            var compteurs = await _context.CommentairesMateriel
                .Where(c => materielIds.Contains(c.MaterielId))
                .GroupBy(c => c.MaterielId)
                .Select(g => new { MaterielId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MaterielId, x => x.Count);

            var groupes = affectations
                .GroupBy(a => a.MaterielId)
                .Select(g =>
                {
                    var premiereMat = g.First().Materiel;
                    var affIds      = g.Select(a => a.Id).ToList();

                    var articlesMatériel = articles
                        .Where(art => affIds.Contains(art.AffectationId!.Value))
                        .ToList();

                    var articlesDto = articlesMatériel.Select(art =>
                    {
                        var aff = g.First(a => a.Id == art.AffectationId);
                        return new ArticleAffecteDto
                        {
                            AffectationId    = aff.Id,
                            ArticleId        = art.Id,
                            NumeroSerie      = art.NumeroSerie ?? $"S/N #{art.Id}",
                            StatutArticle    = art.Statut.ToString(),
                            EtatArticle      = art.Etat.ToString(),
                            StatutBadgeColor = GetEtatColor(art.Etat),
                            DateAffectation  = aff.DateAffectation,
                            Observations     = aff.Observations
                        };
                    }).ToList();

                    var etatDominant = articlesDto.Any(a => a.EtatArticle == "Panne")
                        ? EtatArticle.Panne : EtatArticle.Bon;

                    compteurs.TryGetValue(g.Key, out var nbCommentaires);

                    return new MaterielAffecteGroupeDto
                    {
                        MaterielId          = g.Key,
                        Reference           = premiereMat.Reference,
                        Designation         = premiereMat.Designation,
                        Categorie           = premiereMat.Categorie,
                        ImageUrl            = premiereMat.ImageUrl,
                        NombreArticles      = articlesDto.Count,
                        StatutDominant      = etatDominant.ToString(),
                        StatutBadgeColor    = GetEtatColor(etatDominant),
                        DerniereAffectation = g.Max(a => a.DateAffectation),
                        Articles            = articlesDto,
                        NombreCommentaires  = nbCommentaires   // ← NOUVEAU
                    };
                })
                .OrderByDescending(m => m.DerniereAffectation)
                .ToList();

            return groupes;
        }

        private string GetEtatColor(EtatArticle etat) => etat switch
        {
            EtatArticle.Bon   => "#10B981",
            EtatArticle.Panne => "#EF4444",
            _                 => "#6B7280"
        };
    }
}
