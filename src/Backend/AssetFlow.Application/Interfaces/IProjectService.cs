// ============================================================
// AssetFlow.Application / Interfaces / IProjectService.cs
// ============================================================

using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IProjectService
    {
        Task<List<ProjectDto>> GetAllAsync();
        Task<ProjectDto?> GetByIdAsync(int id);
        Task<ProjectDto> CreateAsync(CreateProjectDto dto);
        Task<ProjectDto?> UpdateAsync(int id, UpdateProjectDto dto);
        Task<bool> DeleteAsync(int id);
        Task<List<AffectationProjetDto>>  GetAffectationsAsync(int projetId);
    }
}