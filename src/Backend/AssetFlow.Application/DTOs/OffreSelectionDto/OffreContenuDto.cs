namespace AssetFlow.Application.DTOs
{
    /// <summary>Contenu détaillé de l'offre (champs OCR + lignes).</summary>
    public class OffreContenuDto
    {
        public string FraisLivraison { get; set; } = string.Empty;
        public string DelaiLivraison { get; set; } = string.Empty;
        public string Garantie       { get; set; } = string.Empty;
        public string TotalHt        { get; set; } = string.Empty;
        public string TotalTva       { get; set; } = string.Empty;
        public string TotalTtc       { get; set; } = string.Empty;
        public List<LigneContenuDto> Lignes { get; set; } = new();
    }

}