namespace AssetFlow.Application.DTOs
{
    public class UtilisateurDisponibleDto
    {
        public int    Id         { get; set; }
        public string FullName   { get; set; } = string.Empty;
        public string Email      { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Initials   { get; set; } = string.Empty;
    }

    public class MaterielDisponibleDto
    {
        public int    Id                 { get; set; }
        public string Reference          { get; set; } = string.Empty;
        public string Designation        { get; set; } = string.Empty;
        public string Categorie          { get; set; } = string.Empty;
        public string? ImageUrl          { get; set; }
        public int    QuantiteDisponible { get; set; }
        public List<ArticleDisponibleDto> Articles { get; set; } = new();
    }

    public class ArticleDisponibleDto
    {
        public int    Id          { get; set; }
        public string NumeroSerie { get; set; } = string.Empty;
        public string Etat        { get; set; } = "Bon";
    }

    public class CreerAffectationDto
    {
        public string    user_name        { get; set; } = string.Empty;
        public int       MaterielId       { get; set; }
        public int?       UtilisateurId    { get; set; }
        public List<int> ArticleIds       { get; set; } = new();
        public string?   Observations     { get; set; }
        public DateTime? DateRetourPrevue { get; set; }

        // ← NOUVEAU : optionnel, renseigné si mode projet
        public int? ProjetId { get; set; }
    }

    public class AffectationResultDto
    {
        public bool   Succes        { get; set; }
        public string Message       { get; set; } = string.Empty;
        public int    AffectationId { get; set; }
    }

    // ← NOUVEAU : DTO projet disponible pour affectation
    public class ProjetDisponibleDto
    {
        public int    Id          { get; set; }
        public string Nom         { get; set; } = string.Empty;
        public string Statut      { get; set; } = string.Empty;
        public string Priorite    { get; set; } = string.Empty;
        public string? Responsable { get; set; }
    }
}