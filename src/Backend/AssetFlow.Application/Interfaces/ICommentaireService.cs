// ============================================================
// AssetFlow.Application / Interfaces / ICommentaireService.cs
// ============================================================

using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface ICommentaireService
    {
        Task<CommentaireResultDto> AjouterCommentaireAsync(CreerCommentaireDto dto);
        Task<List<CommentaireDto>> GetCommentairesMaterielAsync(int materielId);
    }
}
