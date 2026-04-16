namespace AssetFlow.Application.DTOs
{
    public class ChatbotMessageDto
    {
        public string   Role    { get; set; } = string.Empty; // "user" ou "assistant"
        public string   Content { get; set; } = string.Empty;
        public DateTime SentAt  { get; set; } = DateTime.UtcNow;
    }
}