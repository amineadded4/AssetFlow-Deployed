using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IAffectationService
    {
        Task<List<UtilisateurDisponibleDto>> GetUtilisateursDisponiblesAsync(string? search = null);
        Task<List<MaterielDisponibleDto>>    GetMaterielsDisponiblesAsync(string? search = null);
        Task<AffectationResultDto>           CreerAffectationAsync(CreerAffectationDto dto);

        // ← NOUVEAU
        Task<List<ProjetDisponibleDto>> GetProjetsDisponiblesAsync(string? search = null);
    }
}