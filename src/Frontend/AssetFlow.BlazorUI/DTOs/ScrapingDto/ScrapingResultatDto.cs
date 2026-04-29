namespace AssetFlow.BlazorUI.DTOs
{
    public class ScrapingResultatDto
{
    public bool Succes { get; set; }
    public string Query { get; set; } = string.Empty;
    public int NombreResultats { get; set; }
    public string? Erreur { get; set; }
    public string JsonResultat { get; set; } = string.Empty;
}
}