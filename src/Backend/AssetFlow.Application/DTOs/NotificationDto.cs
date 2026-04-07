namespace AssetFlow.Application.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Niveau { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public bool EstLue { get; set; }
        public int? AffectationId { get; set; }
        public int? UtilisateurId { get; set; }
        public string? NomEmploye { get; set; }
        public string? DesignationMateriel { get; set; }
        public DateTime? DateRetourPrevue { get; set; }
 
        /// Nombre de jours de retard (négatif = en avance)
        public int? JoursRetard { get; set; }
    }
}