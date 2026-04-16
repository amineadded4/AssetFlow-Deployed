namespace AssetFlow.Application.DTOs
{
    public class DemandeAchatReponseDto
    {
        public bool   Succes    { get; set; }
        public string Message   { get; set; } = string.Empty;
        public int?   IdDemande { get; set; }
    }
}