namespace AssetFlow.Application.DTOs
{
    public class UpdateDemandeAchatDto
    {
        public string  NomProduit  { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<CreateLigneDemandeDto> Lignes { get; set; } = new();
    }
}