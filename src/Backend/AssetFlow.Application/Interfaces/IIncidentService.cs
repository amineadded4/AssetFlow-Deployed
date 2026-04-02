using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IIncidentService
    {
        Task<SignalerIncidentResponseDto> SignalerIncidentAsync(SignalerIncidentRequestDto request);
        Task<List<IncidentDto>> GetIncidentsByAffectationAsync(int affectationId);
        Task<IncidentDto?> GetIncidentDetailAsync(int incidentId);

        Task<List<IncidentEmployeDto>> GetEmployesAvecIncidentsAsync(string? search = null);
        Task<List<IncidentMaterielDto>> GetMaterielsAvecIncidentsAsync(int utilisateurId);
        Task<SignalerIncidentResponseDto> ChangerStatutAsync(int incidentId, ChangerStatutIncidentDto dto);
        Task<SignalerIncidentResponseDto> ResolveAllByArticleAsync(ResolveAllArticleDto dto);
    }
}