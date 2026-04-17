namespace AssetFlow.Application.DTOs
{
    /// <summary>Nœud du graphe de la mémoire intelligente</summary>
    public class GraphNodeDto
    {
        public string Id       { get; set; } = string.Empty;
        public string Type     { get; set; } = string.Empty; // "materiel" | "incident" | "utilisateur" | "ia" | "commande" | "projet" | "demande" | "commentaire"
        public string Label    { get; set; } = string.Empty;
        public string? Detail  { get; set; }
        public string? Status  { get; set; } // "normal" | "warning" | "critical"
        public int    Weight   { get; set; } = 1;
        public bool   IsCenter { get; set; } = false;
    }
}