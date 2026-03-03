public class ModifierCommandeDto
{
    public int       Id              { get; set; }
    public string    NumeroCommande  { get; set; } = string.Empty;
    public int       FournisseurId   { get; set; }
    public int       QuantiteAchetee { get; set; }
    public DateTime  DateAchat       { get; set; }
    public DateTime? DateLivraison   { get; set; }
    public DateTime? DateFinGarantie { get; set; }
}