using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AssetFlow.WebApi.Controllers
{
    [ApiController]
    [Route("api/demandes")]
    [Authorize(Policy = "AchatOrAdmin")]
    public class DemandesAchatController : ControllerBase
    {
        private readonly IDemandeAchatService _service;

        public DemandesAchatController(IDemandeAchatService service)
        {
            _service = service;
        }

        // GET /api/demandes
        [HttpGet]
        public async Task<ActionResult<List<DemandeAchatDto>>> GetAll()
        {
            var liste = await _service.GetAllAsync();
            return Ok(liste.Select(MapToDto).ToList());
        }

        // GET /api/demandes/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<DemandeAchatDto>> GetById(int id)
        {
            var demande = await _service.GetByIdAsync(id);
            if (demande == null)
                return NotFound(new { Message = $"Demande ID {id} introuvable." });
            return Ok(MapToDto(demande));
        }

        // PUT /api/demandes/{id}/statut
        [HttpPut("{id:int}/statut")]
        public async Task<ActionResult<DemandeAchatReponseDto>> ChangerStatut(
            int id, [FromBody] ChangerStatutDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Statut))
                return BadRequest(new DemandeAchatReponseDto { Succes = false, Message = "Le statut est obligatoire." });

            try
            {
                await _service.ChangerStatutAsync(id, dto.Statut, dto.MotifRefus);
                return Ok(new DemandeAchatReponseDto { Succes = true, Message = $"Statut mis à jour : {dto.Statut}", IdDemande = id });
            }
            catch (KeyNotFoundException ex) { return NotFound(new DemandeAchatReponseDto { Succes = false, Message = ex.Message }); }
            catch (ArgumentException ex)    { return BadRequest(new DemandeAchatReponseDto { Succes = false, Message = ex.Message }); }
        }

        // POST /api/demandes/{id}/offres
        [HttpPost("{id:int}/offres")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult<DemandeAchatReponseDto>> AjouterOffre(int id, IFormFile fichier)
        {
            if (fichier == null || fichier.Length == 0)
                return BadRequest(new DemandeAchatReponseDto { Succes = false, Message = "Aucun fichier reçu." });

            if (!fichier.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new DemandeAchatReponseDto { Succes = false, Message = "Seuls les fichiers PDF sont acceptés." });

            byte[] contenu;
            using (var ms = new MemoryStream())
            {
                await fichier.CopyToAsync(ms);
                contenu = ms.ToArray();
            }

            try
            {
                var offre = new OffreAchat
                {
                    NomFichier = fichier.FileName,
                    Taille     = fichier.Length,
                    ContenuPdf = contenu,
                    EstChoisie = false
                };
                await _service.AjouterOffreAsync(id, offre);
                return StatusCode(201, new DemandeAchatReponseDto { Succes = true, Message = $"Offre « {fichier.FileName} » ajoutée.", IdDemande = id });
            }
            catch (KeyNotFoundException ex) { return NotFound(new DemandeAchatReponseDto { Succes = false, Message = ex.Message }); }
        }

        // DELETE /api/demandes/{id}/offres/{offreId}
        [HttpDelete("{id:int}/offres/{offreId:guid}")]
        public async Task<ActionResult<DemandeAchatReponseDto>> SupprimerOffre(int id, Guid offreId)
        {
            try
            {
                await _service.SupprimerOffreAsync(offreId);
                return Ok(new DemandeAchatReponseDto { Succes = true, Message = "Offre supprimée.", IdDemande = id });
            }
            catch (KeyNotFoundException ex) { return NotFound(new DemandeAchatReponseDto { Succes = false, Message = ex.Message }); }
        }

        // GET /api/demandes/{id}/offres/{offreId}/pdf
        [HttpGet("{id:int}/offres/{offreId:guid}/pdf")]
        public async Task<IActionResult> GetPdf(int id, Guid offreId)
        {
            var contenu = await _service.GetContenuPdfAsync(offreId);
            if (contenu == null || contenu.Length == 0)
                return NotFound(new { Message = "PDF introuvable." });
            return File(contenu, "application/pdf");
        }
        private static DemandeAchatDto MapToDto(DemandeAchat d) => new()
        {
            IdDemande    = d.IdDemande,
            Reference    = d.Reference,
            NomProduit   = d.NomProduit,
            Quantite     = d.Lignes.Any() ? d.Lignes.Sum(l => l.Quantite) : d.Quantite,
            Description  = d.Description,
            Statut       = d.Statut,
            DateCreation = d.DateCreation,
            DemandeurNom = d.DemandeurNom,
            MotifRefus   = d.MotifRefus,
            Lignes = d.Lignes.Select(l => new LigneDemandeDto
            {
                IdLigne     = l.IdLigne,
                Reference   = l.Reference,
                NomProduit  = l.NomProduit,
                Quantite    = l.Quantite,
                Description = l.Description
            }).ToList(),
            Offres = d.Offres.Select(o => new OffreAchatDto
            {
                IdOffre    = o.IdOffre,
                IdDemande  = d.IdDemande,
                NomFichier = o.NomFichier,
                Taille     = o.Taille,
                EstChoisie = o.EstChoisie
            }).ToList()
        };
    }
}
