namespace AssetFlow.BlazorUI.DTOs
{
    public class DemandeAchatDto
    {
        public int      IdDemande    { get; set; }
        public string   Reference    { get; set; } = string.Empty;
        public string   NomProduit   { get; set; } = string.Empty;
        public int      Quantite     { get; set; }
        public string?  Description  { get; set; }
        public string   Statut       { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public string   DemandeurNom { get; set; } = string.Empty;
        public string?  MotifRefus   { get; set; }

        public List<LigneDemandeDto> Lignes { get; set; } = new();
        public List<OffreAchatDto>   Offres { get; set; } = new();
        public DateTime? VuParAchatLe { get; set; }
    }
}