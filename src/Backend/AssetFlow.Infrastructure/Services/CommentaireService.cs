// ============================================================
// AssetFlow.Infrastructure / Services / CommentaireService.cs
// ============================================================

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class CommentaireService : ICommentaireService
    {
        private readonly AppDbContext _context;

        public CommentaireService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CommentaireResultDto> AjouterCommentaireAsync(CreerCommentaireDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Contenu))
                    return new CommentaireResultDto { Succes = false, Message = "Le commentaire ne peut pas être vide." };

                if (dto.Contenu.Length > 1000)
                    return new CommentaireResultDto { Succes = false, Message = "Le commentaire ne doit pas dépasser 1000 caractères." };

                var commentaire = new CommentaireMateriel
                {
                    MaterielId    = dto.MaterielId,
                    UtilisateurId = dto.UtilisateurId,
                    Contenu       = dto.Contenu.Trim(),
                    DateCreation  = DateTime.UtcNow
                };

                _context.CommentairesMateriel.Add(commentaire);
                await _context.SaveChangesAsync();

                return new CommentaireResultDto
                {
                    Succes  = true,
                    Message = "Commentaire enregistré avec succès.",
                    Id      = commentaire.Id
                };
            }
            catch (Exception ex)
            {
                return new CommentaireResultDto { Succes = false, Message = $"Erreur : {ex.Message}" };
            }
        }

        public async Task<List<CommentaireDto>> GetCommentairesMaterielAsync(int materielId)
        {
            var commentaires = await _context.CommentairesMateriel
                .Include(c => c.Utilisateur)
                .Where(c => c.MaterielId == materielId)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();

            return commentaires.Select(c => new CommentaireDto
            {
                Id = c.Id,
                MaterielId      = c.MaterielId,
                UtilisateurId   = c.UtilisateurId,
                AuteurNom       = $"{c.Utilisateur.FirstName} {c.Utilisateur.LastName}",
                AuteurInitiales = $"{(c.Utilisateur.FirstName.Length > 0 ? c.Utilisateur.FirstName[0] : ' ')}{(c.Utilisateur.LastName.Length > 0 ? c.Utilisateur.LastName[0] : ' ')}".ToUpper().Trim(),
                Contenu         = c.Contenu,
                DateCreation    = c.DateCreation
            }).ToList();
        }
    }
}
