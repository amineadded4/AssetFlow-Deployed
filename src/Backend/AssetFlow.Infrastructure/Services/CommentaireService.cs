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
        private readonly IDashboardNotifier _notifier;
        public CommentaireService(AppDbContext context,IDashboardNotifier notifier)
        {
            _context = context;
            _notifier = notifier;
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
                await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
                {
                    Type   = "utilisateur",
                    NodeId = $"u-{dto.UtilisateurId}"
                });

                return new CommentaireResultDto { Succes = true, Message = "Commentaire enregistré.", Id = commentaire.Id };
            }
            catch (Exception ex)
            {
                return new CommentaireResultDto { Succes = false, Message = $"Erreur : {ex.Message}" };
            }
        }
        public async Task<List<CommentaireDto>> GetCommentairesMaterielAsync(int materielId, int userId)
        {
            var commentaires = await _context.CommentairesMateriel
                .Include(c => c.Utilisateur)
                .Where(c => c.MaterielId == materielId && c.UtilisateurId == userId)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();

            return commentaires.Select(c => MapToDto(c)).ToList();
        }
        public async Task<CommentaireResultDto> SupprimerCommentaireAsync(int commentaireId, int utilisateurId)
        {
            try
            {
                var commentaire = await _context.CommentairesMateriel
                    .FirstOrDefaultAsync(c => c.Id == commentaireId);

                if (commentaire == null)
                    return new CommentaireResultDto { Succes = false, Message = "Commentaire introuvable." };

                if (commentaire.UtilisateurId != utilisateurId)
                    return new CommentaireResultDto { Succes = false, Message = "Vous ne pouvez supprimer que vos propres commentaires." };

                _context.CommentairesMateriel.Remove(commentaire);
                await _context.SaveChangesAsync();
                await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
                {
                    Type   = "utilisateur",
                    NodeId = $"u-{utilisateurId}"
                });

                return new CommentaireResultDto { Succes = true, Message = "Commentaire supprimé." };
            }
            catch (Exception ex)
            {
                return new CommentaireResultDto { Succes = false, Message = $"Erreur : {ex.Message}" };
            }
        }

        // ── Lire (vue IT — tous les commentaires) ─────────────────
        public async Task<List<CommentaireITDto>> GetTousLesCommentairesAsync(string? referenceFiltre = null)
        {
            var query = _context.CommentairesMateriel
                .Include(c => c.Utilisateur)
                .Include(c => c.Materiel)
                .AsQueryable();

            // Filtre par référence matériel (insensible à la casse)
            if (!string.IsNullOrWhiteSpace(referenceFiltre))
            {
                var filtre = referenceFiltre.Trim().ToLower();
                query = query.Where(c =>
                    c.Materiel.Reference.ToLower().Contains(filtre) ||
                    c.Materiel.Designation.ToLower().Contains(filtre));
            }

            var commentaires = await query
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();

            return commentaires.Select(c => new CommentaireITDto
            {
                Id                = c.Id,
                MaterielId        = c.MaterielId,
                MaterielRef       = c.Materiel.Reference,
                MaterielNom       = c.Materiel.Designation,
                MaterielCategorie = c.Materiel.Categorie,
                UtilisateurId     = c.UtilisateurId,
                AuteurNom         = $"{c.Utilisateur.FirstName} {c.Utilisateur.LastName}",
                AuteurInitiales   = BuildInitiales(c.Utilisateur.FirstName, c.Utilisateur.LastName),
                AuteurRole        = c.Utilisateur.Role,
                Contenu           = c.Contenu,
                DateCreation      = c.DateCreation
            }).ToList();
        }
        // Suppression de tous les commentaires pour l'it
        public async Task<CommentaireResultDto> SupprimerCommentaireAdminAsync(int commentaireId)
        {
            try
            {
                var commentaire = await _context.CommentairesMateriel
                    .FirstOrDefaultAsync(c => c.Id == commentaireId);
        
                if (commentaire == null)
                    return new CommentaireResultDto { Succes = false, Message = "Commentaire introuvable." };

                var utilisateurId = commentaire.UtilisateurId; 
                _context.CommentairesMateriel.Remove(commentaire);
                await _context.SaveChangesAsync();
                await _notifier.NotifyMemoryAsync("GraphNodeUpdated", new
                {
                    Type   = "utilisateur",
                    NodeId = $"u-{utilisateurId}"
                });
        
                return new CommentaireResultDto { Succes = true, Message = "Commentaire supprimé." };
            }
            catch (Exception ex)
            {
                return new CommentaireResultDto { Succes = false, Message = $"Erreur : {ex.Message}" };
            }
        }
        private static CommentaireDto MapToDto(CommentaireMateriel c) => new()
        {
            Id              = c.Id,
            MaterielId      = c.MaterielId,
            UtilisateurId   = c.UtilisateurId,
            AuteurNom       = $"{c.Utilisateur.FirstName} {c.Utilisateur.LastName}",
            AuteurInitiales = BuildInitiales(c.Utilisateur.FirstName, c.Utilisateur.LastName),
            Contenu         = c.Contenu,
            DateCreation    = c.DateCreation
        };

        private static string BuildInitiales(string first, string last)
        {
            var a = first.Length > 0 ? first[0].ToString() : "";
            var b = last.Length  > 0 ? last[0].ToString()  : "";
            return (a + b).ToUpper().Trim();
        }
    }
}
