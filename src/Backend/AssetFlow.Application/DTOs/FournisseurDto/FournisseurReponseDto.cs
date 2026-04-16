namespace AssetFlow.Application.DTOs
{
     public class FournisseurReponseDto
    {
        public bool Succes { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IdFournisseur { get; set; }
    }
}