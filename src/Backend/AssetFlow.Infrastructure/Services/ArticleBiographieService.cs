using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class ArticleBiographieService : IArticleBiographieService
    {
        private readonly AppDbContext _db;
        private readonly IDashboardNotifier _notifier;

        public ArticleBiographieService(AppDbContext db,IDashboardNotifier notifier)
        {
            _db = db;
            _notifier = notifier;
        }

        public async Task<ArticleBiographieDto?> GetBiographieAsync(int articleId)
        {
            var article = await _db.ArticlesIndividuels
                .Include(a => a.Materiel)
                .Include(a => a.Commande)
                .Include(a => a.Historiques)
                    .ThenInclude(h => h.Utilisateur)
                .FirstOrDefaultAsync(a => a.Id == articleId);

            if (article == null) return null;

            var historiques = article.Historiques
                .OrderBy(h => h.DateEvenement)
                .ToList();

            // Calcul des durées entre événements
            var evenements = historiques.Select((h, i) =>
            {
                int? duree = i == 0 ? null
                    : (int)(h.DateEvenement - historiques[i - 1].DateEvenement).TotalDays;

                return new EvenementArticleDto
                {
                    Id = h.Id,
                    TypeEvenement = h.TypeEvenement.ToString(),
                    DateEvenement = h.DateEvenement,
                    UtilisateurNom = h.Utilisateur != null
                        ? $"{h.Utilisateur.FirstName} {h.Utilisateur.LastName}"
                        : null,
                    Description = h.Description,
                    DureeDepuisPrecedent = duree
                };
            }).ToList();

            // Statistiques
            var personnesDistinctes = historiques
                .Where(h => h.UtilisateurId != null)
                .Select(h => h.UtilisateurId)
                .Distinct()
                .Count();

            var nbIncidents   = historiques.Count(h => h.TypeEvenement == TypeEvenementArticle.PanneDeclaree);
            var nbReparations = historiques.Count(h => h.TypeEvenement == TypeEvenementArticle.Reparation);

            // ── NOUVEAU : Nombre de projets distincts auxquels l'article a participé ──
            var nbProjets = await _db.Affectations
                .Where(a => a.Articles.Any(art => art.Id == articleId) && a.ProjetId != null)
                .Select(a => a.ProjetId)
                .Distinct()
                .CountAsync();
            // ──────────────────────────────────────────────────────────────────────────

            // Jours en stock = durée cumulée des périodes "MiseEnStock"
            int joursEnStock = 0;
            for (int i = 0; i < historiques.Count; i++)
            {
                if (historiques[i].TypeEvenement == TypeEvenementArticle.MiseEnStock)
                {
                    var fin = i + 1 < historiques.Count
                        ? historiques[i + 1].DateEvenement
                        : DateTime.UtcNow;
                    joursEnStock += (int)(fin - historiques[i].DateEvenement).TotalDays;
                }
            }

            // ── CORRIGÉ : Affectation actuelle — utilisateur OU projet ──
            var dernierEvt = historiques.LastOrDefault();
            string? affectationActuelle = null;
            if (dernierEvt?.TypeEvenement == TypeEvenementArticle.Affectation)
            {
                if (dernierEvt.Utilisateur != null)
                {
                    // Affecté à un utilisateur
                    affectationActuelle = $"{dernierEvt.Utilisateur.FirstName} {dernierEvt.Utilisateur.LastName}";
                }
                else if (!string.IsNullOrEmpty(dernierEvt.Description))
                {
                    // Affecté à un projet (description = "Affecté à NomDuProjet")
                    affectationActuelle = dernierEvt.Description
                        .Replace("Affecté à ", "", StringComparison.OrdinalIgnoreCase)
                        .Trim();
                }
            }
            // ─────────────────────────────────────────────────────────────

            // Date d'acquisition = date de la commande liée
            var dateAcquisition = article.Commande?.DateAchat ?? DateTime.UtcNow;

            return new ArticleBiographieDto
            {
                ArticleId           = article.Id,
                NumeroSerie         = article.NumeroSerie ?? $"ART-{article.Id:D4}",
                MaterielReference   = article.Materiel.Reference,
                MaterielDesignation = article.Materiel.Designation,
                MaterielCategorie   = article.Materiel.Categorie,
                DateAcquisition     = dateAcquisition,
                Statut              = article.Statut.ToString(),
                Etat                = article.Etat.ToString(),
                AgeTotalJours       = (int)(DateTime.UtcNow - dateAcquisition).TotalDays,
                NombrePersonnes     = personnesDistinctes,
                NombreIncidents     = nbIncidents,
                NombreReparations   = nbReparations,
                NombreProjets       = nbProjets,   // ← NOUVEAU
                JoursEnStock        = joursEnStock,
                AffectationActuelle = affectationActuelle,
                Historique          = evenements
            };
        }

        public async Task<List<MaterielAvecArticlesDto>> GetMaterielsAvecArticlesAsync()
        {
            var materiels = await _db.Materiels
                .Include(m => m.Affectations)
                .OrderBy(m => m.Designation)
                .ToListAsync();

            var materielIds = materiels.Select(m => m.Id).ToList();

            var articles = await _db.ArticlesIndividuels
                .Include(a => a.Historiques.OrderByDescending(h => h.DateEvenement))
                    .ThenInclude(h => h.Utilisateur)
                .Where(a => materielIds.Contains(a.MaterielId))
                .ToListAsync();

            var articlesParMateriel = articles.GroupBy(a => a.MaterielId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return materiels.Select(m =>
            {
                var arts = articlesParMateriel.GetValueOrDefault(m.Id, new());

                return new MaterielAvecArticlesDto
                {
                    MaterielId  = m.Id,
                    Reference   = m.Reference,
                    Designation = m.Designation,
                    Categorie   = m.Categorie,
                    Articles    = arts.Select(a =>
                    {
                        var dernier = a.Historiques.MaxBy(h => h.DateEvenement);
                        string? affecteA = null;
                        if (dernier?.TypeEvenement == TypeEvenementArticle.Affectation)
                        {
                            if (dernier.Utilisateur != null)
                                affecteA = $"{dernier.Utilisateur.FirstName} {dernier.Utilisateur.LastName}";
                            else if (!string.IsNullOrEmpty(dernier.Description))
                                affecteA = dernier.Description
                                    .Replace("Affecté à ", "", StringComparison.OrdinalIgnoreCase)
                                    .Trim();
                        }

                        return new ArticleResumDto
                        {
                            ArticleId   = a.Id,
                            NumeroSerie = a.NumeroSerie ?? $"ART-{a.Id:D4}",
                            Statut      = a.Statut.ToString(),
                            Etat        = a.Etat.ToString(),
                            AffecteA    = affecteA
                        };
                    }).ToList()
                };
            }).ToList();
        }

        public async Task AjouterEvenementAsync(
            int articleId,
            TypeEvenementArticle typeEvenement,
            int? utilisateurId,
            string? description)
        {
            var evenement = new ArticleHistorique
            {
                ArticleId     = articleId,
                TypeEvenement = typeEvenement,
                UtilisateurId = utilisateurId,
                Description   = description,
                DateEvenement = DateTime.UtcNow
            };

            _db.ArticleHistoriques.Add(evenement);
            await _db.SaveChangesAsync();
            await _notifier.NotifyBiographieAsync(articleId);
        }
    }
}
