namespace AssetFlow.Application.DTOs
{
    public class OffreSelectionDto
    {
        public Guid   OffreId   { get; set; }
        public int    IdDemande { get; set; }
        public string UserId    { get; set; } = string.Empty;
        // Infos OCR à persister
        public string? PrixTotal      { get; set; }
        public string? FraisLivraison { get; set; }
        public string? DelaiLivraison { get; set; }
        public string? Garantie       { get; set; }
        public List<AutreOffreOcrDto> AutresOffres { get; set; } = new();
    }

}