namespace AssetFlow.Application.DTOs
{
    public class CommandeReponseDto
    {
        public bool Succes { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IdCommande { get; set; }
    }
}