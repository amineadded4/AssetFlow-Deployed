// ============================================================
// AssetFlow.WebAPI / Controllers / CommentaireController.cs
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CommentaireController : ControllerBase
    {
        private readonly ICommentaireService _service;

        public CommentaireController(ICommentaireService service)
        {
            _service = service;
        }

        /// <summary>
        /// POST api/commentaire
        /// Enregistre un commentaire employé sur un matériel
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AjouterCommentaire([FromBody] CreerCommentaireDto dto)
        {
            if (dto.MaterielId <= 0 || dto.UtilisateurId <= 0)
                return BadRequest("Données invalides.");

            var result = await _service.AjouterCommentaireAsync(dto);
            if (!result.Succes) return BadRequest(result.Message);

            return Ok(result);
        }

        /// <summary>
        /// GET api/commentaire/materiel/{materielId}
        /// Récupère tous les commentaires d'un matériel
        /// </summary>
        [HttpGet("materiel/{materielId}")]
        public async Task<IActionResult> GetCommentaires(int materielId)
        {
            if (materielId <= 0) return BadRequest("ID matériel invalide.");
            var commentaires = await _service.GetCommentairesMaterielAsync(materielId);
            return Ok(commentaires);
        }
    }
}
