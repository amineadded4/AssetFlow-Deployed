namespace AssetFlow.Application.DTOs
{
    public class ChatMessageDto
    {
        public string Role    { get; set; } = string.Empty; // "user" ou "assistant"
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }

    public class ChatOffreRequestDto
    {
        public string UserId    { get; set; } = string.Empty;
        public int    IdDemande { get; set; }
        public string Message   { get; set; } = string.Empty;
        // Contexte OCR de toutes les offres
        public List<OffreContextDto> Offres { get; set; } = new();
    }

    public class OffreContextDto
    {
        public string NomFichier     { get; set; } = string.Empty;
        public string? PrixTotal      { get; set; }
        public string? DelaiLivraison { get; set; }
        public string? Garantie       { get; set; }
        public string? FraisLivraison { get; set; }
    }
}