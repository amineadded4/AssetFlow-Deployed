using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IEmployeManagementService
    {
        Task<List<EmployeListeDto>> GetEmployesAsync(string? search = null);
        Task<List<AffectationEmployeDto>> GetAffectationsEmployeAsync(int utilisateurId);
        Task<RetirerAffectationResultDto> RetirerAffectationAsync(string userName, int affectationId);
    }
}