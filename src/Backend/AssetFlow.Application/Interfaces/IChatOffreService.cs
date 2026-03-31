// ============================================================
// AssetFlow.Application / Interfaces / IChatOffreService.cs
// ============================================================

using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IChatOffreService
    {
        Task<(string Reply, string? RecommendedOffre)> SendAsync(ChatOffreRequestDto dto);
        Task<List<ChatbotMessageDto>>                  GetHistoryAsync(string userId, int idDemande);
        Task                                           DeleteHistoryAsync(string userId, int idDemande);
        Task<string?>                                  GetRecommendationAsync(string userId, int idDemande);
    }
}