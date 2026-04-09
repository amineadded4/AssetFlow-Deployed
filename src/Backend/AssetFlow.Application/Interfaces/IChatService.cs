using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IChatService
    {
        Task<ChatMessagePayload> SendMessageAsync(int senderId, int receiverId, string content);
        Task<ChatMessagePayload> SendVoiceMessageAsync(int senderId, int receiverId,
                                                       string audioBase64, int durationSeconds);
        Task MarkReadAsync(int readerId, int senderId);
        Task<List<int>> GetOnlineUsersAsync();
    }
}
