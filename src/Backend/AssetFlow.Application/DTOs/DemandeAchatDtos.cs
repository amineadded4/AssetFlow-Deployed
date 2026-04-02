namespace AssetFlow.Application.DTOs
{
    public class LigneDemandeDto
    {
        public int     IdLigne     { get; set; }
        public string  Reference   { get; set; } = string.Empty;
        public string  NomProduit  { get; set; } = string.Empty;
        public int     Quantite    { get; set; }
        public string? Description { get; set; }
    }
    public class DemandeAchatDto
    {
        public int      IdDemande    { get; set; }
        public string   Reference    { get; set; } = string.Empty;
        public string   NomProduit   { get; set; } = string.Empty;
        public int      Quantite     { get; set; }
        public string?  Description  { get; set; }
        public string   Statut       { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public string   DemandeurNom { get; set; } = string.Empty;
        public string?  MotifRefus   { get; set; }

        public List<LigneDemandeDto> Lignes { get; set; } = new();
        public List<OffreAchatDto>   Offres { get; set; } = new();
    }

    public class OffreAchatDto
    {
        public Guid   IdOffre    { get; set; }
        public int    IdDemande  { get; set; }
        public string NomFichier { get; set; } = string.Empty;
        public long   Taille     { get; set; }
        public bool   EstChoisie { get; set; }
           // ── Champs OCR persistés ──
        public string? PrixTotal      { get; set; }
        public string? FraisLivraison { get; set; }
        public string? DelaiLivraison { get; set; }
        public string? Garantie       { get; set; }
    }

    public class DemandeAchatITDto
    {
        public int      IdDemande    { get; set; }
        public string   Reference    { get; set; } = string.Empty;
        public string   NomProduit   { get; set; } = string.Empty;
        public int      Quantite     { get; set; }
        public string?  Description  { get; set; }
        public string   Statut       { get; set; } = "en_attente";
        public DateTime DateCreation { get; set; }
        public string   DemandeurNom { get; set; } = string.Empty;
        public string?  MotifRefus   { get; set; }

        public List<LigneDemandeDto> Lignes { get; set; } = new();
    }
    public class CreateLigneDemandeDto
    {
        public string  Reference   { get; set; } = string.Empty;
        public string  NomProduit  { get; set; } = string.Empty;
        public int     Quantite    { get; set; } = 1;
        public string? Description { get; set; }
    }

    public class CreateDemandeAchatDto
    {
        public int? UserId { get; set; } 
        public string  NomProduit   { get; set; } = string.Empty;
        public string? Description  { get; set; }
        public string? DemandeurNom { get; set; }
        public List<CreateLigneDemandeDto> Lignes { get; set; } = new();
    }

    public class ChangerStatutDto
    {
        public string  Statut     { get; set; } = string.Empty;
        public string? MotifRefus { get; set; }
    }


    public class DemandeAchatReponseDto
    {
        public bool   Succes    { get; set; }
        public string Message   { get; set; } = string.Empty;
        public int?   IdDemande { get; set; }
    }
}
