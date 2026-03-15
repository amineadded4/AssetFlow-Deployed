using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.WebAPI.Controllers
{
    [ApiController]
    [Route("api/projects")]
    [Authorize(Policy = "ITOnly")]  // ← ITOrAdmin au niveau controller
    public class ProjectAffectationsController : ControllerBase
    {
        [HttpGet("{id:int}/affectations")]
        public async Task<IActionResult> GetAffectations(int id, [FromServices] AppDbContext db)
        {
            var affectations = await db.Affectations
                .AsNoTracking()
                .Include(a => a.Materiel)
                .Include(a => a.Articles)
                .Where(a => a.ProjetId == id)
                .OrderByDescending(a => a.DateAffectation)
                .ToListAsync();

            var result = affectations.Select(a => new
            {
                AffectationId    = a.Id,
                MaterielId       = a.MaterielId,
                Designation      = a.Materiel.Designation,
                Reference        = a.Materiel.Reference,
                Categorie        = a.Materiel.Categorie,
                ImageUrl         = a.Materiel.ImageUrl,
                DateAffectation  = a.DateAffectation,
                DateRetourPrevue = a.DateRetour,
                Etat             = a.Etat.ToString(),
                Observations     = a.Observations,
                Articles         = a.Articles.Select(art => new
                {
                    ArticleId   = art.Id,
                    NumeroSerie = art.NumeroSerie ?? $"S/N #{art.Id}",
                    Etat        = art.Etat.ToString()
                }).ToList()
            });

            return Ok(result);
        }
    }
}