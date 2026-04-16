namespace AssetFlow.Application.DTOs
{
    public class LigneMaterielDto
    {
        public int      MaterielId    { get; set; }
        public string   Reference     { get; set; } = string.Empty;
        public string   Designation   { get; set; } = string.Empty;
        public string?  Description   { get; set; }
        public string   Categorie     { get; set; } = string.Empty;
        public int      QuantiteStock { get; set; }
        public int      QuantiteMin   { get; set; }
        public string   Unite         { get; set; } = "pièce";
        public string?  ImageUrl      { get; set; }
        public DateTime DateAjout     { get; set; }

        /// <summary>Toutes les commandes de ce matériel</summary>
        public List<CommandeDto> Commandes { get; set; } = new();
    }
}