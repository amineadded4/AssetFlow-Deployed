namespace AssetFlow.Application.DTOs
{
    public class LigneCommandeMaterielDto
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

        public int       CommandeId      { get; set; }
        public string    NumeroCommande  { get; set; } = string.Empty;
        public int       FournisseurId   { get; set; }
        public string    NomFournisseur  { get; set; } = string.Empty;
        public int       QuantiteAchetee { get; set; }
        public DateTime  DateAchat       { get; set; }
        public DateTime? DateLivraison   { get; set; }
        public DateTime? DateFinGarantie { get; set; }

        public int NbArticles    { get; set; }
        public int NbDisponibles { get; set; }
    }
}