// ============================================================
// AssetFlow.Application / DTOs / ChatMessagePayload.cs
// ============================================================

namespace AssetFlow.Application.DTOs
{
    public class ChatMessagePayload
    {
        public int      Id         { get; set; }
        public int      SenderId   { get; set; }
        public int      ReceiverId { get; set; }
        public string   Content    { get; set; } = string.Empty;
        public DateTime SentAt     { get; set; }
        public bool     IsRead     { get; set; }
    }
}