// ============================================================
// AssetFlow.Application / Interfaces / IIncidentService.cs
// MISE À JOUR : Ajout GetIncidentsByAffectationAsync
// ============================================================

using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    /// <summary>
    /// Service pour la gestion des incidents
    /// </summary>
    public interface IIncidentService
    {
        /// <summary>
        /// Signale un nouvel incident
        /// </summary>
        Task<SignalerIncidentResponseDto> SignalerIncidentAsync(SignalerIncidentRequestDto request);

        /// <summary>
        /// Récupère tous les incidents liés à une affectation
        /// NOUVEAU : utilisé dans la page DetailsEquipement
        /// </summary>
        Task<List<IncidentDto>> GetIncidentsByAffectationAsync(int affectationId);

        /// <summary>
        /// Récupère le détail d'un incident
        /// </summary>
        Task<IncidentDto?> GetIncidentDetailAsync(int incidentId);

        Task<List<IncidentEmployeDto>> GetEmployesAvecIncidentsAsync(string? search = null);
        Task<List<IncidentMaterielDto>> GetMaterielsAvecIncidentsAsync(int utilisateurId);
        Task<SignalerIncidentResponseDto> ChangerStatutAsync(int incidentId, ChangerStatutIncidentDto dto);
        Task<SignalerIncidentResponseDto> ResolveAllByArticleAsync(ResolveAllArticleDto dto);
    }
}