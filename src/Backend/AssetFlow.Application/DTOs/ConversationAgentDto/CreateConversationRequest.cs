namespace AssetFlow.Application.DTOs
{
     public class CreateConversationRequest
    {
        public int    UserId { get; set; }
        public string Title  { get; set; } = "Nouvelle conversation";
    }
}