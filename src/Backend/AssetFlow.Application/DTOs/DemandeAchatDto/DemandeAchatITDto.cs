namespace AssetFlow.Application.DTOs
{
    public class DemandeAchatITDto
    {
        public int      IdDemande    { get; set; }
        public string   Reference    { get; set; } = string.Empty;
        public string   NomProduit   { get; set; } = string.Empty;
        public int      Quantite     { get; set; }
        public string?  Description  { get; set; }
        public string   Statut       { get; set; } = "en_attente";
        public DateTime DateCreation { get; set; }
        public string   DemandeurNom { get; set; } = string.Empty;
        public string?  MotifRefus   { get; set; }

        public List<LigneDemandeDto> Lignes { get; set; } = new();
    }
}