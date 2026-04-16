namespace AssetFlow.Application.DTOs
{
    public class EtatDemandesDto
    {
        public int EnAttente { get; set; }
        public int Commande  { get; set; }
        public int Traite    { get; set; }
        public int Refuse    { get; set; }
    }
}