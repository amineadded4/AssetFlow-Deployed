using AssetFlow.Application.DTOs;
namespace AssetFlow.Application.Interfaces
{
    public interface IOffreSelectionService
    {
        Task<(bool Success, string? Error)> ConfirmSelectionAsync(OffreSelectionDto dto);
    }
}