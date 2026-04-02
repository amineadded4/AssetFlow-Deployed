namespace AssetFlow.Application.DTOs
{
    public class CreerCommentaireDto
    {
        public int    MaterielId    { get; set; }
        public int    UtilisateurId { get; set; }
        public string Contenu       { get; set; } = string.Empty;
    }

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

    public class CommentaireITDto
    {
        public int      Id               { get; set; }
        public int      MaterielId       { get; set; }
        public string   MaterielRef      { get; set; } = string.Empty;   // ex: SN-200
        public string   MaterielNom      { get; set; } = string.Empty;   // ex: PC Dell
        public string   MaterielCategorie { get; set; } = string.Empty;
        public int      UtilisateurId    { get; set; }
        public string   AuteurNom        { get; set; } = string.Empty;
        public string   AuteurInitiales  { get; set; } = string.Empty;
        public string   AuteurRole       { get; set; } = string.Empty;   // Employe, IT, EquipeAchat
        public string   Contenu          { get; set; } = string.Empty;
        public DateTime DateCreation     { get; set; }
    }

    public class CommentaireResultDto
    {
        public bool   Succes  { get; set; }
        public string Message { get; set; } = string.Empty;
        public int?   Id      { get; set; }
    }
}
