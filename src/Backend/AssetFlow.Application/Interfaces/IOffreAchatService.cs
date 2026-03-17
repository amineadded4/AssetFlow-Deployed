// ============================================================
// AssetFlow.Application / Interfaces / IOffreAchatService.cs
// ============================================================

using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IOffreAchatService
    {
        Task<List<OffreAchatDto>> GetByDemandeIdAsync(int demandeId);
        Task<byte[]?>             GetPdfBytesAsync(Guid offreId);
        Task<bool> ChoisirOffreAsync(Guid offreId, int demandeId);
        Task SauvegarderInfosOcrAsync(Guid offreId, string? prix, string? frais, string? delai, string? garantie);
    }
}
