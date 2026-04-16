namespace AssetFlow.Application.DTOs
{
    public class CreateLigneDemandeDto
    {
        public string  Reference   { get; set; } = string.Empty;
        public string  NomProduit  { get; set; } = string.Empty;
        public int     Quantite    { get; set; } = 1;
        public string? Description { get; set; }
    }
}