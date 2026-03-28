// ============================================================
// AssetFlow.WebAPI / Controllers / CommentaireController.cs
// MISE À JOUR : ajout GET api/commentaire/it/tous
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Policy = "ITOrAdmin")]
    public class CommentaireController : ControllerBase
    {
        private readonly ICommentaireService _service;

        public CommentaireController(ICommentaireService service)
        {
            _service = service;
        }

        /// <summary>POST api/commentaire — Enregistre un commentaire</summary>
        [HttpPost]
        public async Task<IActionResult> AjouterCommentaire([FromBody] CreerCommentaireDto dto)
        {
            if (dto.MaterielId <= 0 || dto.UtilisateurId <= 0)
                return BadRequest("Données invalides.");

            var result = await _service.AjouterCommentaireAsync(dto);
            if (!result.Succes) return BadRequest(result.Message);
            return Ok(result);
        }

        /// <summary>GET api/commentaire/materiel/{materielId}/{userId} — Commentaires d'un utilisateur</summary>
        [HttpGet("materiel/{materielId}/{userId}")]
        public async Task<IActionResult> GetCommentaires(int materielId, int userId)
        {
            if (materielId <= 0) return BadRequest("ID matériel invalide.");
            var commentaires = await _service.GetCommentairesMaterielAsync(materielId, userId);
            return Ok(commentaires);
        }

        /// <summary>DELETE api/commentaire/{commentaireId}/{utilisateurId} — Supprime un commentaire (auteur uniquement)</summary>
        [HttpDelete("{commentaireId}/{utilisateurId}")]
        public async Task<IActionResult> SupprimerCommentaire(int commentaireId, int utilisateurId)
        {
            if (commentaireId <= 0 || utilisateurId <= 0)
                return BadRequest("Données invalides.");

            var result = await _service.SupprimerCommentaireAsync(commentaireId, utilisateurId);
            if (!result.Succes) return BadRequest(result.Message);
            return Ok(result);
        }

        /// <summary>
        /// GET api/commentaire/it/tous?reference=SN-200
        /// Vue IT : tous les commentaires, filtrables par référence/désignation.
        /// </summary>
        [HttpGet("it/tous")]
        public async Task<IActionResult> GetTousLesCommentaires([FromQuery] string? reference = null)
        {
            var commentaires = await _service.GetTousLesCommentairesAsync(reference);
            return Ok(commentaires);
        }
        /// <summary>
        /// DELETE api/commentaire/admin/{commentaireId}
        /// Suppression par un agent IT (sans vérification d'auteur)
        /// </summary>
        [HttpDelete("admin/{commentaireId}")]
        public async Task<IActionResult> SupprimerCommentaireAdmin(int commentaireId)
        {
            if (commentaireId <= 0) return BadRequest("ID invalide.");
            var result = await _service.SupprimerCommentaireAdminAsync(commentaireId);
            if (!result.Succes) return BadRequest(result.Message);
            return Ok(result);
        }
    }
}
