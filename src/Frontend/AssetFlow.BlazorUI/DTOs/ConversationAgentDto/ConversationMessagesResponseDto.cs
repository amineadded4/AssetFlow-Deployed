namespace AssetFlow.BlazorUI.DTOs
{
    public class ConversationMessagesResponseDto
        {
            public string                       ConversationId { get; set; } = string.Empty;
            public List<ConversationMessageDto> Messages       { get; set; } = new();
        }
}