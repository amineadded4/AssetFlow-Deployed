namespace AssetFlow.Application.DTOs
{
    public class FournisseurDto
    {
        public int IdFournisseur { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? Telephone { get; set; }
        public string? Adresse { get; set; }
        public string? Mail { get; set; }

        public int CommandesTotales { get; set; }
        public decimal TauxLivraisonATemps { get; set; }
        public decimal ScoreFiabilite { get; set; }
        public DateTime? DerniereCommande { get; set; }
    }

    public class CreerFournisseurDto
    {
        public string Nom { get; set; } = string.Empty;
        public string? Telephone { get; set; }
        public string? Adresse { get; set; }
        public string? Mail { get; set; }

        public int CommandesTotales { get; set; }
        public decimal TauxLivraisonATemps { get; set; }
        public decimal ScoreFiabilite { get; set; }
        public DateTime? DerniereCommande { get; set; }
    }
    public class ModifierFournisseurDto
    {
        public int IdFournisseur { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? Telephone { get; set; }
        public string? Adresse { get; set; }
        public string? Mail { get; set; }

        public int CommandesTotales { get; set; }
        public decimal TauxLivraisonATemps { get; set; }
        public decimal ScoreFiabilite { get; set; }
        public DateTime? DerniereCommande { get; set; }
    }
    public class FournisseurReponseDto
    {
        public bool Succes { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IdFournisseur { get; set; }
    }
}
