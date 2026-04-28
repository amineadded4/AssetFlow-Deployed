namespace AssetFlow.Application.DTOs
{
    public class ConversationListResponse
    {
        public List<ConversationSummaryDto> Conversations { get; set; } = new();
    }
}