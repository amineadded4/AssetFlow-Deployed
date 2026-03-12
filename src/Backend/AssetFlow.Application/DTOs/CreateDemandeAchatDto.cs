// AssetFlow.Application / DTOs / CreateDemandeAchatDto.cs

namespace AssetFlow.Application.DTOs
{
    public class CreateDemandeAchatDto
    {
        public string  NomProduit   { get; set; } = string.Empty;
        public string? Reference    { get; set; }
        public int     Quantite     { get; set; } = 1;
        public string? Description  { get; set; }
        public string? DemandeurNom { get; set; }
    }
}
