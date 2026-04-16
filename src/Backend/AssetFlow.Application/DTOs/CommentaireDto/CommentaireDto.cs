namespace AssetFlow.Application.DTOs
{
    public class CommentaireDto
    {
        public int      Id              { get; set; }
        public int      MaterielId      { get; set; }
        public int      UtilisateurId   { get; set; }
        public string   AuteurNom       { get; set; } = string.Empty;
        public string   AuteurInitiales { get; set; } = string.Empty;
        public string   Contenu         { get; set; } = string.Empty;
        public DateTime DateCreation    { get; set; }
    }
}