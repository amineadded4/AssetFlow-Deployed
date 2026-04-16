namespace AssetFlow.Application.DTOs
{
    public class FournisseurOcrDto
    {
        public string Nom       { get; set; } = string.Empty;
        public string Adresse   { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string Email     { get; set; } = string.Empty;
        public string SiteWeb   { get; set; } = string.Empty;
        public string TvaIntra  { get; set; } = string.Empty;
        public string Iban      { get; set; } = string.Empty;
        public string BicSwift  { get; set; } = string.Empty;
        public string Banque    { get; set; } = string.Empty;
    }
}