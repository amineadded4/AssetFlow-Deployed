using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class IncidentService : IIncidentService
    {
        private readonly AppDbContext _context;
        private readonly IDashboardNotifier _notifier;

        public IncidentService(AppDbContext context, IDashboardNotifier notifier)
        {
            _context = context;
            _notifier = notifier;
        }
        public async Task<SignalerIncidentResponseDto> SignalerIncidentAsync(SignalerIncidentRequestDto request)
        {
            try
            {
                var affectation = await _context.Affectations
                    .Include(a => a.Materiel)
                    .FirstOrDefaultAsync(a => a.Id == request.AffectationId);

                if (affectation == null)
                    return new SignalerIncidentResponseDto { Success = false, Message = "Affectation introuvable." };

                var incident = new Incident
                {
                    AffectationId = request.AffectationId,
                    ArticleId     = request.ArticleId,
                    TypeIncident = request.TypeIncident,
                    Urgence = request.Urgence,
                    Description = request.Description,
                    DateIncident = DateTime.UtcNow,
                    Statut = StatutIncident.EnAttente
                };
                // ← AJOUTER : mettre l'article en Panne si ArticleId fourni
                if (request.ArticleId.HasValue)
                {
                    var article = await _context.ArticlesIndividuels
                        .FindAsync(request.ArticleId.Value);
                    if (article != null)
                    {
                        article.Etat = EtatArticle.Panne;
                    }
                }
                _context.Incidents.Add(incident);
                await _context.SaveChangesAsync();

                var numeroIncident = $"INC-{DateTime.UtcNow.Year}-{incident.Id:D3}";

                await _context.SaveChangesAsync();
                await _notifier.NotifyAsync();
                await _notifier.NotifyITAsync();
                // await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
                // {
                //     Type   = "incident",
                //     NodeId = $"m-{affectation.MaterielId}"
                // });
                await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
                {
                    Type   = "materiel",                        // ← était "incident"
                    NodeId = $"m-{affectation.MaterielId}"
                });
                if (affectation.UtilisateurId.HasValue)
                {
                    await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
                    {
                        Type   = "utilisateur",
                        NodeId = $"u-{affectation.UtilisateurId.Value}"
                    });
                }

                return new SignalerIncidentResponseDto
                {
                    Success = true,
                    Message = "Incident signalé avec succès. L'équipe IT a été notifiée.",
                    IncidentId = incident.Id,
                    NumeroIncident = numeroIncident
                };
            }
            catch (Exception ex)
            {
                return new SignalerIncidentResponseDto
                {
                    Success = false,
                    Message = $"Erreur lors du signalement : {ex.Message}"
                };
            }
        }

        // Récupère tous les incidents liés à une affectation
        public async Task<List<IncidentDto>> GetIncidentsByAffectationAsync(int affectationId)
        {
            var incidents = await _context.Incidents
                .Include(i => i.Affectation)
                .ThenInclude(a => a.Materiel)
                .Where(i => i.AffectationId == affectationId)
                .OrderByDescending(i => i.DateIncident)
                .ToListAsync();

            return incidents.Select(MapToDto).ToList();
        }

        // Récupère le détail d'un incident
        public async Task<IncidentDto?> GetIncidentDetailAsync(int incidentId)
        {
            var incident = await _context.Incidents
                .Include(i => i.Affectation)
                .ThenInclude(a => a.Materiel)
                .FirstOrDefaultAsync(i => i.Id == incidentId);

            return incident == null ? null : MapToDto(incident);
        }
        public async Task<List<IncidentEmployeDto>> GetEmployesAvecIncidentsAsync(string? search = null)
        {
            // Récupérer les utilisateurIds ayant des incidents actifs
            var userIdsAvecIncidents = await _context.Incidents
                .Where(i => i.Statut == StatutIncident.EnAttente || i.Statut == StatutIncident.EnCours)
                .Select(i => i.Affectation.UtilisateurId)
                .Distinct()
                .ToListAsync();

            var query = _context.Users.AsNoTracking()
                .Where(u => u.IsApproved && u.Role != "Admin");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(s) ||
                    u.LastName.ToLower().Contains(s)  ||
                    u.Role.ToLower().Contains(s));
            }

            var users = await query.OrderBy(u => u.FirstName).ToListAsync();

            // Compter incidents actifs par user
            var counts = await _context.Incidents
                .Include(i => i.Affectation)
                .Where(i => (i.Statut == StatutIncident.EnAttente || i.Statut == StatutIncident.EnCours)
                        && i.Affectation.Etat == EtatAffectation.Courante
                        && users.Select(u => u.Id).Contains(i.Affectation.UtilisateurId.Value))
                .GroupBy(i => i.Affectation.UtilisateurId.Value)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();

            return users.Select(u => new IncidentEmployeDto
            {
                UtilisateurId    = u.Id,
                FullName         = $"{u.FirstName} {u.LastName}",
                Role       = u.Role,
                Initials         = $"{u.FirstName[0]}{u.LastName[0]}".ToUpper(),
                NbIncidentsActifs = counts.FirstOrDefault(c => c.UserId == u.Id)?.Count ?? 0
            }).ToList();
        }

        public async Task<List<IncidentMaterielDto>> GetMaterielsAvecIncidentsAsync(int utilisateurId)
        {
            var affectations = await _context.Affectations
                .Include(a => a.Materiel)
                .Include(a => a.Articles)
                .Where(a => a.UtilisateurId == utilisateurId && a.Etat == EtatAffectation.Courante)
                .ToListAsync();

            var affectationIds = affectations.Select(a => a.Id).ToList();

            var incidents = await _context.Incidents
                .Where(i => affectationIds.Contains(i.AffectationId))
                .OrderByDescending(i => i.DateIncident)
                .ToListAsync();

            // Ne retourner que les matériels ayant au moins 1 incident
            return affectations
                .Select(aff =>
                {
                    var incidentsAff = incidents.Where(i => i.AffectationId == aff.Id).ToList();
                    if (!incidentsAff.Any()) return null;

                    var articlesAvecIncidents = aff.Articles
                        .Select(art =>
                        {
                            var incidentsArt = incidentsAff
                                .Where(i => i.ArticleId == art.Id)
                                .Select(MapToDto)
                                .ToList();
                            if (!incidentsArt.Any()) return null;

                            return new IncidentArticleDto
                            {
                                ArticleId   = art.Id,
                                NumeroSerie = art.NumeroSerie ?? $"S/N #{art.Id}",
                                EtatArticle = art.Etat.ToString(),
                                Incidents   = incidentsArt
                            };
                        })
                        .Where(x => x != null)
                        .Cast<IncidentArticleDto>()
                        .ToList();

                    return new IncidentMaterielDto
                    {
                        MaterielId       = aff.MaterielId,
                        AffectationId    = aff.Id,
                        Designation      = aff.Materiel.Designation,
                        Reference        = aff.Materiel.Reference,
                        ImageUrl         = aff.Materiel.ImageUrl,
                        Categorie        = aff.Materiel.Categorie,
                        NbIncidentsActifs = incidentsAff.Count(i =>
                            i.Statut == StatutIncident.EnAttente || i.Statut == StatutIncident.EnCours),
                        Articles         = articlesAvecIncidents
                    };
                })
                .Where(x => x != null)
                .Cast<IncidentMaterielDto>()
                .ToList();
        }

        public async Task<SignalerIncidentResponseDto> ChangerStatutAsync(int incidentId, ChangerStatutIncidentDto dto)
        {
            var incident = await _context.Incidents
                .Include(i => i.Article)
                .FirstOrDefaultAsync(i => i.Id == incidentId);

            if (incident == null)
                return new SignalerIncidentResponseDto { Success = false, Message = "Incident introuvable." };

            if (!Enum.TryParse<StatutIncident>(dto.NouveauStatut, out var nouveauStatut))
                return new SignalerIncidentResponseDto { Success = false, Message = "Statut invalide." };

            incident.Statut = nouveauStatut;

            if (nouveauStatut == StatutIncident.Resolu)
            {
                incident.DateResolution          = DateTime.UtcNow;
                incident.CommentairesResolution  = dto.CommentairesResolution?.Trim();

                // Remettre l'article en Bon si plus aucun incident actif sur cet article
                if (incident.ArticleId.HasValue)
                {
                    var autresIncidentsActifs = await _context.Incidents
                        .AnyAsync(i => i.ArticleId == incident.ArticleId
                                    && i.Id != incidentId
                                    && (i.Statut == StatutIncident.EnAttente || i.Statut == StatutIncident.EnCours));

                    if (!autresIncidentsActifs && incident.Article != null)
                        incident.Article.Etat = EtatArticle.Bon;
                }
            }

            await _context.SaveChangesAsync();
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();
            
            var aff = await _context.Affectations.FindAsync(incident.AffectationId);
            if (aff != null)
            {
                await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
                {
                    Type   = "materiel",
                    NodeId = $"m-{aff.MaterielId}"
                });

                if (aff.UtilisateurId.HasValue)
                {
                    await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
                    {
                        Type   = "utilisateur",
                        NodeId = $"u-{aff.UtilisateurId.Value}"
                    });
                }
            }

            return new SignalerIncidentResponseDto { Success = true, Message = "Statut mis à jour." };
        }

        public async Task<SignalerIncidentResponseDto> ResolveAllByArticleAsync(ResolveAllArticleDto dto)
        {
            var incidents = await _context.Incidents
                .Where(i => i.ArticleId == dto.ArticleId
                        && (i.Statut == StatutIncident.EnAttente || i.Statut == StatutIncident.EnCours))
                .ToListAsync();

            if (!incidents.Any())
                return new SignalerIncidentResponseDto { Success = false, Message = "Aucun incident actif." };

            foreach (var inc in incidents)
            {
                inc.Statut                  = StatutIncident.Resolu;
                inc.DateResolution          = DateTime.UtcNow;
                inc.CommentairesResolution  = dto.CommentairesResolution?.Trim();
            }

            // Remettre l'article en Bon
            var article = await _context.ArticlesIndividuels.FindAsync(dto.ArticleId);
            if (article != null) article.Etat = EtatArticle.Bon;

            await _context.SaveChangesAsync();
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();
            return new SignalerIncidentResponseDto
            {
                Success = true,
                Message = $"{incidents.Count} incident(s) résolus. Article remis en état Bon."
            };
        }

        private IncidentDto MapToDto(Incident incident)
        {
            return new IncidentDto
            {
                Id = incident.Id,
                AffectationId = incident.AffectationId,
                NumeroIncident = $"INC-{incident.DateIncident.Year}-{incident.Id:D3}",
                TypeIncident = incident.TypeIncident,
                Urgence = incident.Urgence,
                UrgenceLabel = GetUrgenceLabel(incident.Urgence),
                Description = incident.Description,
                DateIncident = incident.DateIncident,
                Statut = incident.Statut.ToString(),
                StatutLabel = GetStatutLabel(incident.Statut),
                DateResolution = incident.DateResolution,
                CommentairesResolution = incident.CommentairesResolution,
                MaterielDesignation = incident.Affectation.Materiel.Designation,
                MaterielReference = incident.Affectation.Materiel.Reference
            };
        }

        private string GetUrgenceLabel(int urgence)
        {
            if (urgence <= 33) return "Faible";
            if (urgence <= 66) return "Moyen";
            return "Critique";
        }

        private string GetStatutLabel(StatutIncident statut)
        {
            return statut switch
            {
                StatutIncident.EnAttente => "En attente",
                StatutIncident.EnCours => "En cours",
                StatutIncident.Resolu => "Résolu",
                StatutIncident.Cloture => "Clôturé",
                _ => statut.ToString()
            };
        }
    }
}