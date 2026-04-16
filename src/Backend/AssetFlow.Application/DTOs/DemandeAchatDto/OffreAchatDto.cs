namespace AssetFlow.Application.DTOs
{
    public class OffreAchatDto
    {
        public Guid   IdOffre    { get; set; }
        public int    IdDemande  { get; set; }
        public string NomFichier { get; set; } = string.Empty;
        public long   Taille     { get; set; }
        public bool   EstChoisie { get; set; }
           // ── Champs OCR persistés ──
        public string? PrixTotal      { get; set; }
        public string? FraisLivraison { get; set; }
        public string? DelaiLivraison { get; set; }
        public string? Garantie       { get; set; }
    }
}