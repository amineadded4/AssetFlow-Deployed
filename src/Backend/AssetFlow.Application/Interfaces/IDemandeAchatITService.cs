using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IDemandeAchatITService
    {
        Task<IEnumerable<DemandeAchatITDto>> GetAllAsync(int? userId);
        Task<DemandeAchatITDto?> GetByIdAsync(int id);
        Task<DemandeAchatITDto>  CreateAsync(CreateDemandeAchatDto dto);
        Task<DemandeAchatITDto?> UpdateAsync(int id, UpdateDemandeAchatDto dto);
        Task<bool> DeleteAsync(int id,string userName);
    }
}