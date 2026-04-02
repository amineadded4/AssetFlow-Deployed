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

    public class ArticleDto
    {
        public int Id { get; set; }
        public string? NumeroSerie { get; set; }
        public string Statut { get; set; } = string.Empty;
        public int CommandeId { get; set; }
        public string NumeroCommande { get; set; } = string.Empty;
    }

    public class CreerCommandeDto
    {
        public string NumeroCommande { get; set; } = string.Empty;
        public int MaterielId { get; set; }
        public int FournisseurId { get; set; }
        public int QuantiteAchetee { get; set; }
        public DateTime DateAchat { get; set; } = DateTime.UtcNow;
        public DateTime? DateLivraison { get; set; }
        public DateTime? DateFinGarantie { get; set; }
        public List<string?> NumerosSerie { get; set; } = new();
    }
    public class ModifierCommandeDto
    {
        public int       Id               { get; set; }
        public string    NumeroCommande   { get; set; } = string.Empty;
        public int       FournisseurId    { get; set; }
        /// <summary>Nom libre si le fournisseur n'existe pas encore</summary>
        public string    NomFournisseurLibre { get; set; } = string.Empty;
        public DateTime  DateAchat        { get; set; }
        public DateTime? DateLivraison    { get; set; }
        public DateTime? DateFinGarantie  { get; set; }
    }

    public class CommandeReponseDto
    {
        public bool Succes { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IdCommande { get; set; }
    }
    public class LigneMaterielDto
    {
        // Matériel
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