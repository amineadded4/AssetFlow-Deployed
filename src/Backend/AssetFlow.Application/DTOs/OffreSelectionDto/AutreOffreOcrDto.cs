namespace AssetFlow.Application.DTOs
{
    public class AutreOffreOcrDto
    {
        public Guid    OffreId        { get; set; }
        public string? PrixTotal      { get; set; }
        public string? FraisLivraison { get; set; }
        public string? DelaiLivraison { get; set; }
        public string? Garantie       { get; set; }
    }
}