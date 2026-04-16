namespace AssetFlow.Application.DTOs
{
    public class CommandeDto
    {
        public int Id { get; set; }
        public string NumeroCommande { get; set; } = string.Empty;
        public int MaterielId { get; set; }
        public string NomMateriel { get; set; } = string.Empty;
        public string ReferenceMateriel { get; set; } = string.Empty;
        public int FournisseurId { get; set; }
        public string NomFournisseur { get; set; } = string.Empty;
        public int QuantiteAchetee { get; set; }
        public DateTime DateAchat { get; set; }
        public DateTime? DateLivraison { get; set; }
        public DateTime? DateFinGarantie { get; set; }
        public List<ArticleDto> Articles { get; set; } = new();
    }
}