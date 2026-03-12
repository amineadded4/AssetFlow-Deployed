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
    }
}
