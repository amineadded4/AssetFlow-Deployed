namespace AssetFlow.BlazorUI.DTOs
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
        public int? JoursRetard { get; set; }

        /// Calcul local du temps relatif
        public string TempsRelatif
        {
            get
            {
                var diff = DateTime.UtcNow - DateCreation;
                if (diff.TotalMinutes < 1)  return "À l'instant";
                if (diff.TotalMinutes < 60) return $"Il y a {(int)diff.TotalMinutes} min";
                if (diff.TotalHours   < 24) return $"Il y a {(int)diff.TotalHours} h";
                if (diff.TotalDays    < 7)  return $"Il y a {(int)diff.TotalDays} j";
                return DateCreation.ToString("dd/MM/yyyy");
            }
        }

        public bool EstCritique   => Niveau == "Critique";
        public bool EstExpire     => Type == "AffectationExpiree";
        public bool EstPreventif  => Type == "RetourEnRetard";
    }
}