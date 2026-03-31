// ============================================================
// AssetFlow.Application / Interfaces / IChatService.cs
// ============================================================

using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IChatService
    {
        Task<ChatMessagePayload> SendMessageAsync(int senderId, int receiverId, string content);
        Task MarkReadAsync(int readerId, int senderId);
        Task<List<int>> GetOnlineUsersAsync();
    }
}