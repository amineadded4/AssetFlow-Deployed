// ============================================================
// COUCHE  : WebApi  (projet API séparé)
// FICHIER : Controllers/FournisseursController.cs
// RÔLE    : Endpoints REST CRUD pour les fournisseurs.
//           Reçoit les requêtes HTTP, délègue au service,
//           retourne des réponses avec les DTOs appropriés.
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller REST — Fournisseurs
    /// Base URL : /api/fournisseurs
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FournisseursController : ControllerBase
    {
        // IFournisseurService injecté — jamais la classe concrète directement
        private readonly IFournisseurService _service;

        public FournisseursController(IFournisseurService service)
        {
            _service = service;
        }

        // ────────────────────────────────────────────────────────
        // GET /api/fournisseurs
        // Retourne tous les fournisseurs
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Retourne la liste complète des fournisseurs.
        /// Réponse 200 OK + List&lt;FournisseurDto&gt;
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<FournisseurDto>>> GetAll()
        {
            var liste = await _service.GetAllAsync();

            // Mapper entités → DTOs avant d'envoyer au client
            var dtos = liste.Select(MapToDto).ToList();

            return Ok(dtos);
        }

        // ────────────────────────────────────────────────────────
        // GET /api/fournisseurs/{id}
        // Retourne un fournisseur par IdFournisseur
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Retourne un fournisseur par son identifiant.
        /// Réponse 200 OK ou 404 Not Found.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<FournisseurDto>> GetById(int id)
        {
            var fournisseur = await _service.GetByIdAsync(id);

            if (fournisseur == null)
                return NotFound(new { Message = $"Fournisseur ID {id} introuvable." });

            return Ok(MapToDto(fournisseur));
        }

        // ────────────────────────────────────────────────────────
        // GET /api/fournisseurs/recherche?terme=xxx
        // Recherche par Nom / Telephone / Adresse / Mail
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Recherche des fournisseurs contenant le terme dans leurs champs.
        /// Réponse 200 OK + liste (peut être vide).
        /// </summary>
        [HttpGet("recherche")]
        public async Task<ActionResult<List<FournisseurDto>>> Rechercher(
            [FromQuery] string terme)
        {
            if (string.IsNullOrWhiteSpace(terme))
                return BadRequest(new { Message = "Le terme de recherche est requis." });

            var liste = await _service.RechercherAsync(terme);
            return Ok(liste.Select(MapToDto).ToList());
        }

        // ────────────────────────────────────────────────────────
        // POST /api/fournisseurs
        // Crée un nouveau fournisseur
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Crée un nouveau fournisseur à partir du DTO reçu.
        /// Réponse 201 Created + FournisseurReponseDto avec l'ID généré.
        /// </summary>
        [HttpPost]
public async Task<ActionResult<FournisseurReponseDto>> Ajouter(CreerFournisseurDto dto)
{
    var entite = new Fournisseur
    {
        Nom = dto.Nom.Trim(),
        Telephone = dto.Telephone?.Trim(),
        Adresse = dto.Adresse?.Trim(),
        Mail = dto.Mail?.Trim(),

        CommandesTotales = dto.CommandesTotales,
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

        // ────────────────────────────────────────────────────────
        // PUT /api/fournisseurs/{id}
        // Met à jour un fournisseur existant
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Modifie un fournisseur existant.
        /// Réponse 200 OK ou 404 Not Found.
        /// </summary>
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

        CommandesTotales = dto.CommandesTotales,
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

        // ────────────────────────────────────────────────────────
        // DELETE /api/fournisseurs/{id}
        // Supprime un fournisseur
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Supprime définitivement un fournisseur par son ID.
        /// Réponse 200 OK ou 404 Not Found.
        /// </summary>
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

        // ────────────────────────────────────────────────────────
        // MÉTHODE PRIVÉE — Mapper Entité → DTO
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Convertit une entité Domain en DTO de lecture.
        /// Centralise la logique de mapping dans le controller.
        /// </summary>
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
