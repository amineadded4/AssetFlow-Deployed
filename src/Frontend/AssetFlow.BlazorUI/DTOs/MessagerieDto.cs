namespace AssetFlow.BlazorUI.DTOs
{
    public class ITUserConvDto
    {
        public int       Id              { get; set; }
        public string    FullName        { get; set; } = string.Empty;
        public string    Initials        { get; set; } = string.Empty;
        public string?   LastMessage     { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int       UnreadCount     { get; set; }
        public bool      IsOnline        { get; set; }
        public bool      IsTyping        { get; set; }
    }

    /*public class ChatMessageDto
    {
        public int      Id         { get; set; }
        public int      SenderId   { get; set; }
        public int      ReceiverId { get; set; }
        public string   Content    { get; set; } = string.Empty;
        public DateTime SentAt     { get; set; }
        public bool     IsRead     { get; set; }
    }*/

    public class ConversationSummaryDto
    {
        public int      OtherUserId     { get; set; }
        public string?  LastMessage     { get; set; }
        public DateTime LastMessageTime { get; set; }
        public int      UnreadCount     { get; set; }
    }

    // Messageri chez Achat
    public class ITUserForAchatDto
    {
        public int       Id              { get; set; }
        public string    FullName        { get; set; } = string.Empty;
        public string    Initials        { get; set; } = string.Empty;
        public string?   LastMessage     { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int       UnreadCount     { get; set; }
        public bool      IsOnline        { get; set; }
        public bool      IsTyping        { get; set; }
    }

    // Messagerie chez IT
    public class ConversationDto
    {
        public int       EmployeId        { get; set; }
        public string    FullName         { get; set; } = string.Empty;
        public string    Initials         { get; set; } = string.Empty;
        public string    Role             { get; set; } = string.Empty;

        public string?   LastMessage      { get; set; }
        public DateTime? LastMessageTime  { get; set; }
        public int       UnreadCount      { get; set; }
        public bool      IsOnline         { get; set; }
        public bool      IsTyping         { get; set; }
    }
    public class AchatUserConvDto
    {
        public int       Id              { get; set; }
        public string    FullName        { get; set; } = string.Empty;
        public string    Initials        { get; set; } = string.Empty;
        public string?   LastMessage     { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int       UnreadCount     { get; set; }
        public bool      IsOnline        { get; set; }
        public bool      IsTyping        { get; set; }
    }
}