namespace AssetFlow.BlazorUI.DTOs
{
    public class ChatMessageDto
    {
        public int      Id                   { get; set; }
        public int      SenderId             { get; set; }
        public int      ReceiverId           { get; set; }
        public string   Content              { get; set; } = string.Empty;
        public DateTime SentAt               { get; set; }
        public bool     IsRead               { get; set; }

        // ── Message vocal ──────────────────────────────────────────────────
        public string?  AudioData            { get; set; } = null;
        public int      AudioDurationSeconds { get; set; } = 0;
        public bool     IsVoice              => AudioData != null;
        // ──────────────────────────────────────────────────────────────────
    }
}
