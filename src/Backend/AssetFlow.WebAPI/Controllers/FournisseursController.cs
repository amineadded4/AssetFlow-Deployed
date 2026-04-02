using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Controllers
{
    // Base URL : /api/fournisseurs
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AchatOrAdmin")]

    public class FournisseursController : ControllerBase
    {
        // IFournisseurService injecté — jamais la classe concrète directement
        private readonly IFournisseurService _service;

        public FournisseursController(IFournisseurService service)
        {
            _service = service;
        }

        // GET /api/fournisseurs
        [HttpGet]
        public async Task<ActionResult<List<FournisseurDto>>> GetAll()
        {
            var liste = await _service.GetAllAsync();

            // Mapper entités → DTOs avant d'envoyer au client
            var dtos = liste.Select(MapToDto).ToList();

            return Ok(dtos);
        }

        // GET /api/fournisseurs/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<FournisseurDto>> GetById(int id)
        {
            var fournisseur = await _service.GetByIdAsync(id);

            if (fournisseur == null)
                return NotFound(new { Message = $"Fournisseur ID {id} introuvable." });

            return Ok(MapToDto(fournisseur));
        }
        // GET /api/fournisseurs/recherche?terme=xxx
        [HttpGet("recherche")]
        public async Task<ActionResult<List<FournisseurDto>>> Rechercher(
            [FromQuery] string terme)
        {
            if (string.IsNullOrWhiteSpace(terme))
                return BadRequest(new { Message = "Le terme de recherche est requis." });

            var liste = await _service.RechercherAsync(terme);
            return Ok(liste.Select(MapToDto).ToList());
        }

        // POST /api/fournisseurs
        [HttpPost]
public async Task<ActionResult<FournisseurReponseDto>> Ajouter(CreerFournisseurDto dto)
{
    var entite = new Fournisseur
    {
        Nom = dto.Nom.Trim(),
        Telephone = dto.Telephone?.Trim(),
        Adresse = dto.Adresse?.Trim(),
        Mail = dto.Mail?.Trim(),

        CommandesTotales = 0,
        TauxLivraisonATemps = dto.TauxLivraisonATemps,
        ScoreFiabilite = dto.ScoreFiabilite,
        DerniereCommande = dto.DerniereCommande
    };

    var cree = await _service.AjouterAsync(entite);

    return Ok(new FournisseurReponseDto
    {
        Succes = true,
        Message = "Fournisseur ajouté avec succès.",
        IdFournisseur = cree.IdFournisseur
    });
}

        // PUT /api/fournisseurs/{id}
[HttpPut("{id:int}")]
public async Task<ActionResult<FournisseurReponseDto>> Modifier(ModifierFournisseurDto dto)
{
    var entite = new Fournisseur
    {
        IdFournisseur = dto.IdFournisseur,
        Nom = dto.Nom.Trim(),
        Telephone = dto.Telephone?.Trim(),
        Adresse = dto.Adresse?.Trim(),
        Mail = dto.Mail?.Trim(),

        TauxLivraisonATemps = dto.TauxLivraisonATemps,
        ScoreFiabilite = dto.ScoreFiabilite,
        DerniereCommande = dto.DerniereCommande
    };

    await _service.ModifierAsync(entite);

    return Ok(new FournisseurReponseDto
    {
        Succes = true,
        Message = "Fournisseur modifié avec succès.",
        IdFournisseur = dto.IdFournisseur
    });
}

        // DELETE /api/fournisseurs/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<FournisseurReponseDto>> Supprimer(int id)
        {
            try
            {
                await _service.SupprimerAsync(id);

                return Ok(new FournisseurReponseDto
                {
                    Succes  = true,
                    Message = $"Fournisseur ID {id} supprimé avec succès."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new FournisseurReponseDto
                {
                    Succes  = false,
                    Message = ex.Message
                });
            }
        }
private static FournisseurDto MapToDto(Fournisseur f) => new()
{
    IdFournisseur = f.IdFournisseur,
    Nom = f.Nom,
    Telephone = f.Telephone,
    Adresse = f.Adresse,
    Mail = f.Mail,

    CommandesTotales = f.CommandesTotales,
    TauxLivraisonATemps = f.TauxLivraisonATemps,
    ScoreFiabilite = f.ScoreFiabilite,
    DerniereCommande = f.DerniereCommande
};
    }
}
