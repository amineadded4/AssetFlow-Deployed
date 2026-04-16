namespace AssetFlow.Application.DTOs
{
    public class ModifierCommandeDto
    {
        public string Utilisateur { get; set; } = string.Empty;
        public int       Id               { get; set; }
        public string    NumeroCommande   { get; set; } = string.Empty;
        public int       FournisseurId    { get; set; }
        /// <summary>Nom libre si le fournisseur n'existe pas encore</summary>
        public string    NomFournisseurLibre { get; set; } = string.Empty;
        public DateTime  DateAchat        { get; set; }
        public DateTime? DateLivraison    { get; set; }
        public DateTime? DateFinGarantie  { get; set; }
    }
}