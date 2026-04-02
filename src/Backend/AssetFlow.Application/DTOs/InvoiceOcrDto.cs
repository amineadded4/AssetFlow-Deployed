// Mirrors the JSON structure extracted by Llama4
namespace AssetFlow.Application.DTOs
{
    public class InvoiceOcrDto
    {
        public FournisseurOcrDto             Fournisseur               { get; set; } = new();
        public ClientOcrDto                  Client                    { get; set; } = new();
        public FactureOcrDto                 Facture                   { get; set; } = new();
        public InfosAdditionnellesOcrDto     InformationsAdditionnelles{ get; set; } = new();
        public List<LigneOcrDto>             Lignes                    { get; set; } = new();
        public TotauxOcrDto                  Totaux                    { get; set; } = new();
    }

    public class FournisseurOcrDto
    {
        public string Nom       { get; set; } = string.Empty;
        public string Adresse   { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string Email     { get; set; } = string.Empty;
        public string SiteWeb   { get; set; } = string.Empty;
        public string TvaIntra  { get; set; } = string.Empty;
        public string Iban      { get; set; } = string.Empty;
        public string BicSwift  { get; set; } = string.Empty;
        public string Banque    { get; set; } = string.Empty;
    }

    public class ClientOcrDto
    {
        public string Nom     { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
    }

    public class FactureOcrDto
    {
        public string Numero         { get; set; } = string.Empty;
        public string Date           { get; set; } = string.Empty;
        public string Echeance       { get; set; } = string.Empty;
        public string Paiement       { get; set; } = string.Empty;
        public string Reference      { get; set; } = string.Empty;
        public string NumeroCommande { get; set; } = string.Empty;
    }

    public class InfosAdditionnellesOcrDto
    {
        public string Garantie       { get; set; } = string.Empty;
        public string DelaiLivraison { get; set; } = string.Empty;
        public string FraisLivraison { get; set; } = string.Empty;
    }

    public class LigneOcrDto
    {
        public string Description    { get; set; } = string.Empty;
        public string Quantite       { get; set; } = string.Empty;
        public string Unite          { get; set; } = string.Empty;
        public string PrixUnitaireHt { get; set; } = string.Empty;
        public string TvaPct         { get; set; } = string.Empty;
        public string TotalTva       { get; set; } = string.Empty;
        public string TotalTtc       { get; set; } = string.Empty;
    }

    public class TotauxOcrDto
    {
        public string TotalHt  { get; set; } = string.Empty;
        public string TotalTva { get; set; } = string.Empty;
        public string TotalTtc { get; set; } = string.Empty;
    }
}