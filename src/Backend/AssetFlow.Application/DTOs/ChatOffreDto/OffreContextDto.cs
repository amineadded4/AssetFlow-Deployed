namespace AssetFlow.Application.DTOs
{
    public class OffreContextDto
    {
        public string  NomFichier     { get; set; } = string.Empty;
        public string? PrixTotal      { get; set; }
        public string? DelaiLivraison { get; set; }
        public string? Garantie       { get; set; }
        public string? FraisLivraison { get; set; }
    }
}