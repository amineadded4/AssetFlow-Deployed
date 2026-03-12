// ============================================================
// AssetFlow.Application / Interfaces / IDemandeAchatITService.cs
// ============================================================

using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IDemandeAchatITService
    {
        /// <summary>Retourne toutes les demandes d'achat visibles par l'IT.</summary>
        Task<IEnumerable<DemandeAchatITDto>> GetAllAsync();

        /// <summary>Retourne une demande d'achat par son identifiant.</summary>
        Task<DemandeAchatITDto?> GetByIdAsync(int id);

        /// <summary>Crée une nouvelle demande d'achat depuis l'interface IT.</summary>
        Task<DemandeAchatITDto> CreateAsync(CreateDemandeAchatDto dto);
    }
}
