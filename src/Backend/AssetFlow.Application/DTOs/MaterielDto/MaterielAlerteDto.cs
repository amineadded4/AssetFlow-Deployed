namespace AssetFlow.Application.DTOs
{
    public class MaterielAlerteDto
{
    public string Reference   { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Categorie   { get; set; } = string.Empty;
    public int    QuantiteStock { get; set; }
    public int    QuantiteMin   { get; set; }
    public string Emplacement  { get; set; } = "—";
    public string Niveau       { get; set; } = string.Empty; // "Critique" ou "Alerte"
}
}