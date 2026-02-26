// ============================================================
// AssetFlow.Infrastructure / Services / IncidentService.cs
// MISE À JOUR : Ajout GetIncidentsByAffectationAsync
// ============================================================

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

        public IncidentService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Signale un nouvel incident
        /// </summary>
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

        /// <summary>
        /// NOUVEAU : Récupère tous les incidents liés à une affectation
        /// </summary>
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

        /// <summary>
        /// Récupère le détail d'un incident
        /// </summary>
        public async Task<IncidentDto?> GetIncidentDetailAsync(int incidentId)
        {
            var incident = await _context.Incidents
                .Include(i => i.Affectation)
                .ThenInclude(a => a.Materiel)
                .FirstOrDefaultAsync(i => i.Id == incidentId);

            return incident == null ? null : MapToDto(incident);
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