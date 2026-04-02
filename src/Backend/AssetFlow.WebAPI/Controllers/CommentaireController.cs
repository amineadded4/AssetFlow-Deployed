using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentaireController : ControllerBase
    {
        private readonly ICommentaireService _service;

        public CommentaireController(ICommentaireService service)
        {
            _service = service;
        }

        // POST api/commentaire — Enregistre un commentaire
        [HttpPost]
        public async Task<IActionResult> AjouterCommentaire([FromBody] CreerCommentaireDto dto)
        {
            if (dto.MaterielId <= 0 || dto.UtilisateurId <= 0)
                return BadRequest("Données invalides.");

            var result = await _service.AjouterCommentaireAsync(dto);
            if (!result.Succes) return BadRequest(result.Message);
            return Ok(result);
        }

        // GET api/commentaire/materiel/{materielId}/{userId} — Commentaires d'un utilisateur
        [HttpGet("materiel/{materielId}/{userId}")]
        public async Task<IActionResult> GetCommentaires(int materielId, int userId)
        {
            if (materielId <= 0) return BadRequest("ID matériel invalide.");
            var commentaires = await _service.GetCommentairesMaterielAsync(materielId, userId);
            return Ok(commentaires);
        }

        // >DELETE api/commentaire/{commentaireId}/{utilisateurId} — Supprime un commentaire (auteur uniquement)
        [HttpDelete("{commentaireId}/{utilisateurId}")]
        public async Task<IActionResult> SupprimerCommentaire(int commentaireId, int utilisateurId)
        {
            if (commentaireId <= 0 || utilisateurId <= 0)
                return BadRequest("Données invalides.");

            var result = await _service.SupprimerCommentaireAsync(commentaireId, utilisateurId);
            if (!result.Succes) return BadRequest(result.Message);
            return Ok(result);
        }
        // GET api/commentaire/it/tous?reference=SN-200
        [HttpGet("it/tous")]
        public async Task<IActionResult> GetTousLesCommentaires([FromQuery] string? reference = null)
        {
            var commentaires = await _service.GetTousLesCommentairesAsync(reference);
            return Ok(commentaires);
        }
        // DELETE api/commentaire/admin/{commentaireId}
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
