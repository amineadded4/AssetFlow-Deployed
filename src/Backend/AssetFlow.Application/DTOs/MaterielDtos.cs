// ============================================================
// AssetFlow.Application / DTOs / MaterielDtos.cs
// DTO d'entrée/sortie pour la gestion du matériel
// ============================================================

namespace AssetFlow.Application.DTOs
{
    // ── Réponse générique ──────────────────────────────────────
    /// <summary>Résultat retourné par les opérations d'écriture</summary>
    public class MaterielResultDto
    {
        public bool   Succes    { get; set; }
        public string Message   { get; set; } = string.Empty;
        public int?   IdMateriel { get; set; }
    }

    // ── Lecture ────────────────────────────────────────────────
    /// <summary>Données complètes d'un matériel (lecture seule)</summary>
    public class MaterielDto
    {
        public int      Id            { get; set; }
        public string   Reference     { get; set; } = string.Empty;
        public string   Designation   { get; set; } = string.Empty;
        public string?  Description   { get; set; }
        public string   Categorie     { get; set; } = string.Empty;
        public int      QuantiteStock { get; set; }
        public int      QuantiteMin   { get; set; }
        public string   Unite         { get; set; } = "pièce";
        public string?  Emplacement   { get; set; }
        public string   Etat          { get; set; } = "Disponible";
        public string?  ImageUrl      { get; set; }
        public DateTime DateAjout     { get; set; }
    }

    // ── Création ───────────────────────────────────────────────
    /// <summary>Données nécessaires pour créer un matériel</summary>
    public class CreerMaterielDto
    {
        public string   Reference     { get; set; } = string.Empty;
        public string   Designation   { get; set; } = string.Empty;
        public string?  Description   { get; set; }
        public string   Categorie     { get; set; } = string.Empty;
        public int      QuantiteStock { get; set; }
        public int      QuantiteMin   { get; set; }
        public string   Unite         { get; set; } = "pièce";
        public string?  Emplacement   { get; set; }
        public string   Etat          { get; set; } = "Disponible";
        public string?  ImageUrl      { get; set; }
    }

    // ── Modification ───────────────────────────────────────────
    /// <summary>Données pour mettre à jour un matériel existant</summary>
    public class ModifierMaterielDto
    {
        public int      Id            { get; set; }
        public string   Reference     { get; set; } = string.Empty;
        public string   Designation   { get; set; } = string.Empty;
        public string?  Description   { get; set; }
        public string   Categorie     { get; set; } = string.Empty;
        public int      QuantiteStock { get; set; }
        public int      QuantiteMin   { get; set; }
        public string   Unite         { get; set; } = "pièce";
        public string?  Emplacement   { get; set; }
        public string   Etat          { get; set; } = "Disponible";
        public string?  ImageUrl      { get; set; }
    }
        public class MaterielStatsDto
    {
        public int TotalArticles   { get; set; }
        public int EnStock         { get; set; }
        public int AlerteSeuil     { get; set; }
        public int RuptureCritique { get; set; }
    }
}