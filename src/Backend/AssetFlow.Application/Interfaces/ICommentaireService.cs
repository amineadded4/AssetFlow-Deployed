using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface ICommentaireService
    {
        Task<CommentaireResultDto>   AjouterCommentaireAsync(CreerCommentaireDto dto);
        Task<List<CommentaireDto>>   GetCommentairesMaterielAsync(int materielId, int userId);
        Task<CommentaireResultDto>   SupprimerCommentaireAsync(int commentaireId, int utilisateurId);

        // Vue IT : tous les commentaires, filtrables par référence matériel.
        Task<List<CommentaireITDto>> GetTousLesCommentairesAsync(string? referenceFiltre = null);
        //Suppression de commentaires par IT
        Task<CommentaireResultDto> SupprimerCommentaireAdminAsync(int commentaireId);
    }
}
