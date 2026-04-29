// src/Backend/AssetFlow.Application/DTOs/AgentDtos/DemandeAgentDtos.cs
  namespace AssetFlow.Application.DTOs.AgentDtos
  {
      public class DemandePendingDto
      {
          public int      IdDemande    { get; set; }
          public string   Reference    { get; set; } = string.Empty;
          public string   NomProduit   { get; set; } = string.Empty;
          public int      Quantite     { get; set; }
          public string?  Description  { get; set; }
          public string   Statut       { get; set; } = "en_attente";
          public DateTime DateCreation { get; set; }
          public string   DemandeurNom { get; set; } = string.Empty;
          public List<LigneDemandeMini> Lignes { get; set; } = new();
      }

      public class LigneDemandeMini
      {
          public string  Reference   { get; set; } = string.Empty;
          public string  NomProduit  { get; set; } = string.Empty;
          public int     Quantite    { get; set; }
          public string? Description { get; set; }
      }

      public class OffreSearchResultDto
      {
          public string  Id              { get; set; } = Guid.NewGuid().ToString("N")[..8];
          public string  Fournisseur     { get; set; } = string.Empty;
          public string  NomProduit      { get; set; } = string.Empty;
          public string? Description     { get; set; }
          public string? PrixUnitaire    { get; set; }
          public string? PrixTotal       { get; set; }
          public string? FraisLivraison  { get; set; }
          public string? DelaiLivraison  { get; set; }
          public string? Garantie        { get; set; }
          public string? Url             { get; set; }
          public string? Devise          { get; set; } = "MAD";
          public List<string> PointsForts { get; set; } = new();
      }

      public class SelectOfferRequest
      {
          public OffreSearchResultDto Offre { get; set; } = new();
      }

      // ── NOUVEAU : Groupe d'offres pour UN matériel d'une demande ────────────
      /// <summary>
      /// Représente les 4 offres trouvées pour un matériel donné d'une demande
      /// d'achat multi-lignes. Un onglet par groupe sera affiché côté UI.
      /// </summary>
      public class MaterielOffersGroup
      {
          public string Reference  { get; set; } = string.Empty;
          public string NomProduit { get; set; } = string.Empty;
          public int    Quantite   { get; set; }
          public string? Description { get; set; }
          public List<OffreSearchResultDto> Offres { get; set; } = new();
      }
  }
  