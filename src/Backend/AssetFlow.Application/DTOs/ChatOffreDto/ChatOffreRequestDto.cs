namespace AssetFlow.Application.DTOs
{
    public class ChatOffreRequestDto
    {
        public string                UserId    { get; set; } = string.Empty;
        public int                   IdDemande { get; set; }
        public string                Message   { get; set; } = string.Empty;
        public List<OffreContextDto> Offres    { get; set; } = new();
    }
}