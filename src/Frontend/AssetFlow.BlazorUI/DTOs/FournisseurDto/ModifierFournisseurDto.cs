namespace AssetFlow.BlazorUI.DTOs
{
    public class ModifierFournisseurDto
    {
        public int IdFournisseur { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? Telephone { get; set; }
        public string? Adresse { get; set; }
        public string? Mail { get; set; }

        public int CommandesTotales { get; set; }
        public DateTime? DerniereCommande { get; set; }
    }
}