// ============================================================
// AssetFlow.Application / DTOs / OffreAchatDto.cs
// ============================================================

namespace AssetFlow.Application.DTOs
{
    public class OffreAchatDto
    {
        public Guid   IdOffre    { get; set; }
        public int    IdDemande  { get; set; }
        public string NomFichier { get; set; } = string.Empty;
        public long   Taille     { get; set; }
        public bool   EstChoisie { get; set; }
    }
}
