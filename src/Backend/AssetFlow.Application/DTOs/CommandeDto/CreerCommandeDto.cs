namespace AssetFlow.Application.DTOs
{
     public class CreerCommandeDto
    {
        public string Utilisateur { get; set; } = string.Empty;
        public string NumeroCommande { get; set; } = string.Empty;
        public int MaterielId { get; set; }
        public int FournisseurId { get; set; }
        public int QuantiteAchetee { get; set; }
        public DateTime DateAchat { get; set; } = DateTime.UtcNow;
        public DateTime? DateLivraison { get; set; }
        public DateTime? DateFinGarantie { get; set; }
        public List<string?> NumerosSerie { get; set; } = new();
    }
}