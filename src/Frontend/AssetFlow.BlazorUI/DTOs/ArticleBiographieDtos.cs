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

    public class EvenementArticleDto
    {
        public int Id { get; set; }
        public string TypeEvenement { get; set; } = string.Empty;
        public DateTime DateEvenement { get; set; }
        public string? UtilisateurNom { get; set; }
        public string? Description { get; set; }
        public int? DureeDepuisPrecedent { get; set; }
    }

    public class MaterielAvecArticlesDto
    {
        public int MaterielId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Categorie { get; set; } = string.Empty;
        public List<ArticleResumDto> Articles { get; set; } = new();
    }

    public class ArticleResumDto
    {
        public int ArticleId { get; set; }
        public string NumeroSerie { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public string Etat { get; set; } = string.Empty;
        public string? AffecteA { get; set; }
    }
}
