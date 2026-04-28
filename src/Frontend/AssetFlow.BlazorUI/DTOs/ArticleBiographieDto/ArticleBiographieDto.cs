namespace AssetFlow.BlazorUI.DTOs
{
    public class ArticleBiographieDto
    {
        public int ArticleId { get; set; }
        public string NumeroSerie { get; set; } = string.Empty;
        public string MaterielReference { get; set; } = string.Empty;
        public string MaterielDesignation { get; set; } = string.Empty;
        public string MaterielCategorie { get; set; } = string.Empty;
        public DateTime DateAcquisition { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string Etat { get; set; } = string.Empty;
        public int AgeTotalJours { get; set; }
        public int NombrePersonnes { get; set; }
        public int NombreIncidents { get; set; }
        public int NombreReparations { get; set; }
        public int     NombreProjets       { get; set; } 
        public int JoursEnStock { get; set; }
        public string? AffectationActuelle { get; set; }
        public List<EvenementArticleDto> Historique { get; set; } = new();
    }
}