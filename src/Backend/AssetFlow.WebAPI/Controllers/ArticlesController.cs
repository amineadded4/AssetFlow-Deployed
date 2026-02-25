// ============================================================
// AssetFlow.WebAPI / Controllers / ArticlesController.cs
// Gestion des articles individuels (update numéro de série)
// ============================================================

using AssetFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/articles")]
    public class ArticlesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ArticlesController(AppDbContext db) => _db = db;

        // PATCH api/articles/{id}/numero-serie
        [HttpPatch("{id:int}/numero-serie")]
        public async Task<IActionResult> UpdateNumeroSerie(int id, [FromBody] UpdateNumeroSerieDto dto)
        {
            var article = await _db.ArticlesIndividuels.FindAsync(id);
            if (article is null)
                return NotFound(new { Message = "Article introuvable." });

            // Vérifier unicité du numéro de série (si non null)
            if (!string.IsNullOrWhiteSpace(dto.NumeroSerie))
            {
                var ns = dto.NumeroSerie.Trim();
                var existe = await _db.ArticlesIndividuels
                    .AnyAsync(a => a.NumeroSerie == ns && a.Id != id);
                if (existe)
                    return BadRequest(new { Message = "Ce numéro de série est déjà utilisé." });

                article.NumeroSerie = ns;
            }
            else
            {
                article.NumeroSerie = null;
            }

            await _db.SaveChangesAsync();
            return Ok(new { Message = "Numéro de série mis à jour.", NumeroSerie = article.NumeroSerie });
        }
    }

    public class UpdateNumeroSerieDto
    {
        public string? NumeroSerie { get; set; }
    }
}