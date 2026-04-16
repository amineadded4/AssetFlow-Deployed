namespace AssetFlow.Application.DTOs
{
    public class LigneDemandeDto
    {
        public int     IdLigne     { get; set; }
        public string  Reference   { get; set; } = string.Empty;
        public string  NomProduit  { get; set; } = string.Empty;
        public int     Quantite    { get; set; }
        public string? Description { get; set; }
    }
}