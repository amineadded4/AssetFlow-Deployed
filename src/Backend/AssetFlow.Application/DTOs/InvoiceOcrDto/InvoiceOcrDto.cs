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
}