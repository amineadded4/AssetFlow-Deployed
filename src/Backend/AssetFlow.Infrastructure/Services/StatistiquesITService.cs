using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class StatistiquesITService : IStatistiquesITService
    {
        private readonly AppDbContext _db;

        public StatistiquesITService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardITStatsDto> GetDashboardITAsync()
        {
            // ── Matériels & articles ─────────────────────────────
            var totalMateriels = await _db.Materiels.CountAsync();
            var totalArticles  = await _db.ArticlesIndividuels.CountAsync();

            var articlesParStatut = await _db.ArticlesIndividuels
                .GroupBy(a => a.Statut)
                .Select(g => new { Statut = g.Key, Count = g.Count() })
                .ToListAsync();

            var statMap = articlesParStatut.ToDictionary(x => x.Statut, x => x.Count);

            var articlesDto = new ArticleStatutDto
            {
                Disponible   = statMap.GetValueOrDefault(StatutArticle.Disponible,   0),
                Affecte      = statMap.GetValueOrDefault(StatutArticle.Affecte,      0),
                HorsService  = statMap.GetValueOrDefault(StatutArticle.HorsService,  0),
                EnReparation = statMap.GetValueOrDefault(StatutArticle.EnReparation, 0),
            };

            // ── Incidents ─────────────────────────────────────────
            var incidentsActifs = await _db.Incidents
                .CountAsync(i => i.Statut == StatutIncident.EnAttente || i.Statut == StatutIncident.EnCours);

            var incidentParStatut = new IncidentStatutCountDto
            {
                EnAttente = await _db.Incidents.CountAsync(i => i.Statut == StatutIncident.EnAttente),
                EnCours   = await _db.Incidents.CountAsync(i => i.Statut == StatutIncident.EnCours),
                Resolu    = await _db.Incidents.CountAsync(i => i.Statut == StatutIncident.Resolu),
                Cloture   = await _db.Incidents.CountAsync(i => i.Statut == StatutIncident.Cloture),
            };

            var incidentsParType = await _db.Incidents
                .Where(i => !string.IsNullOrEmpty(i.TypeIncident))
                .GroupBy(i => i.TypeIncident)
                .Select(g => new IncidentTypeCountDto { Type = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            // ── Affectations ──────────────────────────────────────
            var affectationsEnCours = await _db.Affectations
                .CountAsync(a => a.Etat == EtatAffectation.Courante);

            // Affectations par département (via utilisateur)
            var affectationsParDept = await _db.Affectations
                .Where(a => a.Etat == EtatAffectation.Courante && a.Utilisateur != null)
                .Include(a => a.Utilisateur)
                .GroupBy(a => a.Utilisateur!.Department)
                .Select(g => new AffectationDepartementDto
                {
                    Departement = g.Key ?? "Inconnu",
                    Count       = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            // ── Équipements par catégorie ─────────────────────────
            var equipementsParCategorie = await _db.Materiels
                .GroupBy(m => m.Categorie)
                .Select(g => new
                {
                    Categorie = g.Key,
                    Total     = g.Count(),
                })
                .ToListAsync();

            var affectesParCategorie = await _db.ArticlesIndividuels
                .Where(a => a.Statut == StatutArticle.Affecte)
                .Include(a => a.Materiel)
                .GroupBy(a => a.Materiel.Categorie)
                .Select(g => new { Categorie = g.Key, Count = g.Count() })
                .ToListAsync();

            var affectesMap = affectesParCategorie.ToDictionary(x => x.Categorie, x => x.Count);

            var equipDtos = equipementsParCategorie.Select(e => new CategorieEquipementDto
            {
                Categorie = e.Categorie,
                Total     = e.Total,
                Affectes  = affectesMap.GetValueOrDefault(e.Categorie, 0),
            }).ToList();

            // ── Demandes d'achat en attente ───────────────────────
            var demandesEnAttente = await _db.DemandeAchat
                .CountAsync(d => d.Statut == "en_attente");

            // ── Incidents bruts (12 dernières semaines) ───────────
            var dateLimite = new DateTime(DateTime.UtcNow.Year - 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var incidentsRaw = await _db.Incidents
                .Include(i => i.Affectation)
                .Where(i => i.DateIncident >= dateLimite)
                .Select(i => new IncidentRawDto
                {
                    DateIncident   = i.DateIncident,
                    DateResolution = i.DateResolution,
                    Statut         = i.Statut.ToString(),
                    TypeIncident   = i.TypeIncident,
                    Urgence        = i.Urgence,
                    MaterielId     = i.Affectation.MaterielId,
                })
                
                .ToListAsync();

            // ── Tendance résolution (8 dernières semaines) ─────────
            var tendanceResolution = new List<ResolutionTempsDto>();
            var cursor = DateTime.UtcNow.AddDays(-56).Date;
            for (int w = 0; w < 8; w++)
            {
                var ws = cursor;
                var we = cursor.AddDays(6);
                var resolved = incidentsRaw
                    .Where(i => i.DateResolution.HasValue
                             && i.DateResolution.Value.Date >= ws
                             && i.DateResolution.Value.Date <= we)
                    .ToList();

                double avg = resolved.Any()
                    ? resolved.Average(i => (i.DateResolution!.Value - i.DateIncident).TotalHours)
                    : 0;

                int wn = System.Globalization.ISOWeek.GetWeekOfYear(ws);
                tendanceResolution.Add(new ResolutionTempsDto
                {
                    Label         = $"S{wn:D2}",
                    MoyenneHeures = Math.Round(avg, 1),
                });
                cursor = cursor.AddDays(7);
            }
            var materielsDisponibles = await _db.Materiels
                .OrderBy(m => m.Designation)
                .Select(m => new MaterielHeatmapDto { MaterielId = m.Id, Designation = m.Designation })
                .ToListAsync();


            return new DashboardITStatsDto
            {
                TotalMateriels          = totalMateriels,
                TotalArticles           = totalArticles,
                IncidentsActifs         = incidentsActifs,
                AffectationsEnCours     = affectationsEnCours,
                ArticlesHorsService     = articlesDto.HorsService + articlesDto.EnReparation,
                DemandesAchatEnAttente  = demandesEnAttente,
                IncidentParStatut       = incidentParStatut,
                IncidentsParType        = incidentsParType,
                ArticlesParStatut       = articlesDto,
                AffectationsParDept     = affectationsParDept,
                EquipementsParCategorie = equipDtos,
                TendanceResolution      = tendanceResolution,
                IncidentsRaw            = incidentsRaw,
                MaterielsDisponibles = materielsDisponibles,
            };
        }
    }
}
