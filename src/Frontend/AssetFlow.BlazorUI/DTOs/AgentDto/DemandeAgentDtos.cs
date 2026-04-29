namespace AssetFlow.BlazorUI.DTOs
{
    public class DemandePendingDto
    {
        public int      IdDemande    { get; set; }
        public string   Reference    { get; set; } = string.Empty;
        public string   NomProduit   { get; set; } = string.Empty;
        public int      Quantite     { get; set; }
        public string?  Description  { get; set; }
        public string   Statut       { get; set; } = "en_attente";
        public DateTime DateCreation { get; set; }
        public string   DemandeurNom { get; set; } = string.Empty;
        public List<LigneDemandeMini> Lignes { get; set; } = new();
    }

    public class LigneDemandeMini
    {
        public string  Reference   { get; set; } = string.Empty;
        public string  NomProduit  { get; set; } = string.Empty;
        public int     Quantite    { get; set; }
        public string? Description { get; set; }
    }

    public class OffreSearchResultDto
    {
        public string  Id              { get; set; } = string.Empty;
        public string  Fournisseur     { get; set; } = string.Empty;
        public string  NomProduit      { get; set; } = string.Empty;
        public string? Description     { get; set; }
        public string? PrixUnitaire    { get; set; }
        public string? PrixTotal       { get; set; }
        public string? FraisLivraison  { get; set; }
        public string? DelaiLivraison  { get; set; }
        public string? Garantie        { get; set; }
        public string? Url             { get; set; }
        public string? Devise          { get; set; } = "MAD";
        public List<string> PointsForts { get; set; } = new();
    }

    public class SelectOfferRequest
    {
        public OffreSearchResultDto Offre { get; set; } = new();
    }
}
