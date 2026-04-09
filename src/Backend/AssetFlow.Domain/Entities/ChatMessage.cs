namespace AssetFlow.Domain.Entities
{
    public class ChatMessage
    {
        public int      Id         { get; set; }
        public int      SenderId   { get; set; }
        public int      ReceiverId { get; set; }
        public string   Content    { get; set; } = string.Empty;
        public DateTime SentAt     { get; set; } = DateTime.UtcNow;
        public bool     IsRead     { get; set; } = false;

        // ── Message vocal ──────────────────────────────────────────────
        // Audio stocké en base64 (webm/ogg), null si message texte
        public string? AudioData            { get; set; } = null;
        public int     AudioDurationSeconds { get; set; } = 0;
        // ──────────────────────────────────────────────────────────────

        public User? Sender   { get; set; }
        public User? Receiver { get; set; }
    }
}
