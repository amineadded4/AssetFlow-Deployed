namespace AssetFlow.Application.DTOs
{
    public class OffreSelectionDto
    {
        public Guid   OffreId   { get; set; }
        public int    IdDemande { get; set; }
        public string UserId    { get; set; } = string.Empty;
        // Infos OCR à persister
        public string? PrixTotal      { get; set; }
        public string? FraisLivraison { get; set; }
        public string? DelaiLivraison { get; set; }
        public string? Garantie       { get; set; }
        public List<AutreOffreOcrDto> AutresOffres { get; set; } = new();
    }
    public class AutreOffreOcrDto
    {
        public Guid    OffreId        { get; set; }
        public string? PrixTotal      { get; set; }
        public string? FraisLivraison { get; set; }
        public string? DelaiLivraison { get; set; }
        public string? Garantie       { get; set; }
    }

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

    public class LigneContenuDto
    {
        public string Description    { get; set; } = string.Empty;
        public string Quantite       { get; set; } = string.Empty;
        public string Unite          { get; set; } = string.Empty;
        public string PrixUnitaireHt { get; set; } = string.Empty;
        public string TvaPct         { get; set; } = string.Empty;
        public string TotalTva       { get; set; } = string.Empty;
        public string TotalTtc       { get; set; } = string.Empty;
    }
}