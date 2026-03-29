// ============================================================
// AssetFlow.Application / Interfaces / IDemandeAchatITService.cs
// AJOUT : UpdateAsync + DeleteAsync
// ============================================================

using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IDemandeAchatITService
    {
        Task<IEnumerable<DemandeAchatITDto>> GetAllAsync(int? userId);
        Task<DemandeAchatITDto?> GetByIdAsync(int id);
        Task<DemandeAchatITDto>  CreateAsync(CreateDemandeAchatDto dto);

        /// <summary>Modifie le titre, la description et les lignes d'une demande.</summary>
        Task<DemandeAchatITDto?> UpdateAsync(int id, UpdateDemandeAchatDto dto);

        /// <summary>Supprime la demande et toutes ses offres associées.</summary>
        Task<bool> DeleteAsync(int id);
    }
}