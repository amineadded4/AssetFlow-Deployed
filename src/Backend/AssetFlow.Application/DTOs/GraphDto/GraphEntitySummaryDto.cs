namespace AssetFlow.Application.DTOs
{
     /// <summary>Résumé d'une entité pour le panneau gauche de la mémoire intelligente</summary>
    public class GraphEntitySummaryDto
    {
        public string Id     { get; set; } = string.Empty;
        public string Label  { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string Type   { get; set; } = string.Empty; // "materiel" | "utilisateur" | "demande" | "projet"
        public string Status { get; set; } = "normal";     // "normal" | "warning" | "critical"
        public int    Count  { get; set; } = 0;            // incidents ou offres ou matériels selon type
    }
}