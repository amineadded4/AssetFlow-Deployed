// ============================================================
// AssetFlow.Infrastructure / Services / EmployeService.cs
// MISE À JOUR : ajout GetMaterielsGroupesAsync
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

        // ── Existant : liste plate ─────────────────────────────
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

        public async Task<EquipementAffecteDto?> GetEquipementDetailAsync(int affectationId)
        {
            var affectation = await _context.Affectations
                .Include(a => a.Materiel)
                .FirstOrDefaultAsync(a => a.Id == affectationId);

            if (affectation == null) return null;

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
                Observations     = affectation.Observations
            };
        }

        // ── NOUVEAU : matériels groupés ────────────────────────
        /// <summary>
        /// Retourne les matériels distincts affectés à l'employé,
        /// chacun avec la liste de ses articles (affectations EnCours).
        ///
        public async Task<List<MaterielAffecteGroupeDto>> GetMaterielsGroupesAsync(int utilisateurId)
        {
            var affectations = await _context.Affectations
                .Include(a => a.Materiel)
                .Where(a => a.UtilisateurId == utilisateurId)
                .OrderByDescending(a => a.DateAffectation)
                .ToListAsync();

            if (!affectations.Any())
                return new List<MaterielAffecteGroupeDto>();

            var materielIds = affectations.Select(a => a.MaterielId).Distinct().ToList();
            
            // ← CORRECTION : supprimer le filtre Affecte, prendre tous les articles
            var articles = await _context.ArticlesIndividuels
                .Where(art => materielIds.Contains(art.MaterielId))
                .ToListAsync();

            var groupes = affectations
                .GroupBy(a => a.MaterielId)
                .Select(g =>
                {
                    var premiereMat = g.First().Materiel;
                    var articlesMatériel = articles
                        .Where(art => art.MaterielId == g.Key)
                        .ToList();

                    var articlesDto = new List<ArticleAffecteDto>();

                    foreach (var affectation in g)
                    {
                        var article = articlesMatériel.FirstOrDefault(
                            art => !articlesDto.Any(dto => dto.ArticleId == art.Id));

                        if (article != null)
                        {
                            // Marquer comme affecté
                            article.Statut = StatutArticle.Affecte;
                        }

                        articlesDto.Add(new ArticleAffecteDto
                        {
                            AffectationId   = affectation.Id,
                            ArticleId       = article?.Id ?? 0,
                            NumeroSerie     = article?.NumeroSerie ?? "S/N non renseigné",
                            StatutArticle   = article?.Statut.ToString() ?? "Disponible",
                            // ← BADGE selon Etat
                            EtatArticle     = article?.Etat.ToString() ?? "Bon",
                            StatutBadgeColor = GetEtatColor(article?.Etat ?? EtatArticle.Bon),
                            DateAffectation = affectation.DateAffectation,
                            Observations    = affectation.Observations
                        });
                    }

                    var etatDominant = articlesDto.Any(a => a.EtatArticle == "Panne")
                        ? EtatArticle.Panne : EtatArticle.Bon;

                    return new MaterielAffecteGroupeDto
                    {
                        MaterielId          = g.Key,
                        Reference           = premiereMat.Reference,
                        Designation         = premiereMat.Designation,
                        Categorie           = premiereMat.Categorie,
                        ImageUrl            = premiereMat.ImageUrl,
                        NombreArticles      = articlesMatériel.Count, // ← vrais articles
                        StatutDominant      = etatDominant.ToString(),
                        StatutBadgeColor    = GetEtatColor(etatDominant),
                        DerniereAffectation = g.Max(a => a.DateAffectation),
                        Articles            = articlesDto
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

        // ── Helpers ────────────────────────────────────────────

    }
}