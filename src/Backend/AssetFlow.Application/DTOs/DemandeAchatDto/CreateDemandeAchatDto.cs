namespace AssetFlow.Application.DTOs
{
    public class CreateDemandeAchatDto
    {
        public string Utilisateur   { get; set; } = string.Empty;
        public int? UserId { get; set; } 
        public string  NomProduit   { get; set; } = string.Empty;
        public string? Description  { get; set; }
        public string? DemandeurNom { get; set; }
        public List<CreateLigneDemandeDto> Lignes { get; set; } = new();
    }
}