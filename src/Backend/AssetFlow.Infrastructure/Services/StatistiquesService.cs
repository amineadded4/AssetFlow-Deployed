using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class StatistiquesService : IStatistiquesService
    {
        private readonly AppDbContext _db;
        public StatistiquesService(AppDbContext db) => _db = db;

        public async Task<DashboardStatsDto> GetDashboardAsync(
            int annee, int? moisDebut, int? moisFin)
        {
            // Charger tout en parallèle
            var materiels    = await _db.Materiels.AsNoTracking().ToListAsync();
            var commandes    = await _db.Commandes.AsNoTracking().ToListAsync();
            var articles     = await _db.ArticlesIndividuels.AsNoTracking().ToListAsync();
            var demandes     = await _db.DemandeAchat.AsNoTracking().ToListAsync();
            var affectations = await _db.Affectations.AsNoTracking().ToListAsync();

            // KPIs
            var totalDemandesActives = demandes
                .Count(d => d.Statut == "en_attente" || d.Statut == "commande");

            // ── Affectation matériel ──────────────────────────────
            var idsAffectes = affectations.Select(a => a.MaterielId).Distinct().ToHashSet();
            var affectation = new AffectationMaterielDto
            {
                Affecte    = materiels.Count(m => idsAffectes.Contains(m.Id)),
                NonAffecte = materiels.Count(m => !idsAffectes.Contains(m.Id)),
            };

            // ── Articles par matériel ─────────────────────────────
            var articlesParMateriel = articles
                .GroupBy(a => a.MaterielId)
                .Select(g =>
                {
                    var mat = materiels.FirstOrDefault(m => m.Id == g.Key);
                    return new ArticlesParMaterielDto
                    {
                        Designation  = mat?.Designation ?? "—",
                        Disponibles  = g.Count(a => a.Statut == StatutArticle.Disponible),
                        Affectes     = g.Count(a => a.Statut == StatutArticle.Affecte),
                        HorsService  = g.Count(a => a.Statut == StatutArticle.HorsService),
                        EnReparation = g.Count(a => a.Statut == StatutArticle.EnReparation),
                    };
                })
                .OrderByDescending(x => x.Disponibles + x.Affectes)
                .Take(15)
                .ToList();

            // ── Articles par CATÉGORIE ────────────────────────────
            // Joindre articles → materiels pour obtenir la catégorie
            var articlesParCategorie = (
                from art in articles
                join mat in materiels on art.MaterielId equals mat.Id
                group new { art, mat } by mat.Categorie into g
                select new ArticlesParCategorieDto
                {
                    Categorie    = string.IsNullOrWhiteSpace(g.Key) ? "Non défini" : g.Key,
                    Disponibles  = g.Count(x => x.art.Statut == StatutArticle.Disponible),
                    Affectes     = g.Count(x => x.art.Statut == StatutArticle.Affecte),
                    HorsService  = g.Count(x => x.art.Statut == StatutArticle.HorsService),
                    EnReparation = g.Count(x => x.art.Statut == StatutArticle.EnReparation),
                }
            ).OrderByDescending(x => x.Disponibles + x.Affectes).ToList();

            // ── Demandes brutes (pour filtrage côté Blazor) ───────
            var demandesRaw = demandes.Select(d => new DemandeRawDto
            {
                DateCreation = d.DateCreation,
                Statut       = d.Statut,
            }).ToList();

            return new DashboardStatsDto
            {
                TotalMateriels       = materiels.Count,
                TotalCommandes       = commandes.Count,
                TotalArticles        = articles.Count,
                TotalDemandesActives = totalDemandesActives,
                AffectationMateriel  = affectation,
                ArticlesParMateriel  = articlesParMateriel,
                ArticlesParCategorie = articlesParCategorie,
                DemandesRaw          = demandesRaw,
            };
        }
    }
}
