namespace AssetFlow.Application.DTOs
{
    public class CreerCommentaireDto
    {
        public int    MaterielId    { get; set; }
        public int    UtilisateurId { get; set; }
        public string Contenu       { get; set; } = string.Empty;
    }
}