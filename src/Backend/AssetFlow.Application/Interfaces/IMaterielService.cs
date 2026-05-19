using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IMaterielService
    {
        Task<IEnumerable<MaterielDto>> GetAllAsync();
        Task<MaterielDto?> GetByIdAsync(int id);
        Task<IEnumerable<MaterielDto>> SearchAsync(string? terme, string? categorie);
        Task<MaterielResultDto> CreerAsync(CreerMaterielDto dto);
        Task<MaterielResultDto> ModifierAsync(ModifierMaterielDto dto);
        Task<MaterielResultDto> SupprimerAsync(string userName,int id);

        /// <summary>Supprime un matériel + ses affectations + les incidents de ces affectations</summary>
        Task<MaterielResultDto> SupprimerAvecCascadeAsync(string userName,int id);

        Task<MaterielStatsDto> GetStatsAsync();
        Task<IEnumerable<MaterielAlerteDto>> GetAlertesStockAsync();
    }
}