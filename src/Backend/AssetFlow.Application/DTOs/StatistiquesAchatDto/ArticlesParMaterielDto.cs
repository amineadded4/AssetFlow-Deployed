namespace AssetFlow.Application.DTOs
{
    public class ArticlesParMaterielDto
    {
        public string Designation  { get; set; } = string.Empty;
        public int    Disponibles  { get; set; }
        public int    Affectes     { get; set; }
        public int    HorsService  { get; set; }
        public int    EnReparation { get; set; }
    }
}