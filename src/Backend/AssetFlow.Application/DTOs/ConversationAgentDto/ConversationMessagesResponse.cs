namespace AssetFlow.Application.DTOs
{
    public class ConversationMessagesResponse
    {
        public string                      ConversationId { get; set; } = string.Empty;
        public List<ConversationMessageDto> Messages      { get; set; } = new();
    }
}