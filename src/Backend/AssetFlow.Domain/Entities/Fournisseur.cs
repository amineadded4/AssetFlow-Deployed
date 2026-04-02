namespace AssetFlow.Domain.Entities
{
    public class Fournisseur
    {
        public int IdFournisseur { get; set; }
        public string Nom { get; set; } = string.Empty;

        public string? Telephone { get; set; }
        public string? Adresse { get; set; }
        public string? Mail { get; set; }
        public int CommandesTotales { get; set; } = 0;
        public decimal TauxLivraisonATemps { get; set; } = 0;
        public decimal ScoreFiabilite { get; set; } = 0;
        public DateTime? DerniereCommande { get; set; }
    }
}
        
        