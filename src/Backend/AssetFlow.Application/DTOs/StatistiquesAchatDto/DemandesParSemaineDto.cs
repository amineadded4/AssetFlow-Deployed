namespace AssetFlow.Application.DTOs
{
    public class DemandesParSemaineDto
    {
        public string Label     { get; set; } = string.Empty;
        public int    EnAttente { get; set; }
        public int    Commande  { get; set; }
        public int    Traite    { get; set; }
    }
}