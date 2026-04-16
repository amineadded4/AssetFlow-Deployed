namespace AssetFlow.Application.DTOs
{
    public class FactureOcrDto
    {
        public string Numero         { get; set; } = string.Empty;
        public string Date           { get; set; } = string.Empty;
        public string Echeance       { get; set; } = string.Empty;
        public string Paiement       { get; set; } = string.Empty;
        public string Reference      { get; set; } = string.Empty;
        public string NumeroCommande { get; set; } = string.Empty;
    }
}