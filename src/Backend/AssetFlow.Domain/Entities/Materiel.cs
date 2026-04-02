namespace AssetFlow.Domain.Entities
{
    public class Materiel
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Categorie { get; set; } = string.Empty;
        public int QuantiteStock { get; set; }
        public int QuantiteMin { get; set; }
        public string Unite { get; set; } = "pièce";
        public string? Emplacement { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime DateAjout { get; set; } = DateTime.UtcNow;
        public ICollection<Affectation> Affectations { get; set; } = new List<Affectation>();
    }
}