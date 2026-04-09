using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface ICommandeService
    {
        Task<IEnumerable<CommandeDto>>              GetAllAsync();
        Task<IEnumerable<CommandeDto>>              GetByMaterielAsync(int materielId);
        Task<CommandeDto?>                          GetByIdAsync(int id);

        // UNE LIGNE PAR MATERIEL avec ses commandes imbriquées
        Task<IEnumerable<LigneMaterielDto>>         GetLignesMaterielsAsync();
        Task<IEnumerable<LigneCommandeMaterielDto>> GetLignesCommandesAsync();

        Task<IEnumerable<ArticleDto>>               GetArticlesByMaterielAsync(int materielId);
        Task<IEnumerable<ArticleDto>>               GetArticlesByCommandeAsync(int commandeId);

        Task<CommandeReponseDto> CreerAsync(CreerCommandeDto dto);
        Task<CommandeReponseDto> ModifierAsync(ModifierCommandeDto dto);

        Task<CommandeReponseDto> SupprimerAsync(string utilisateur, int id);
    }
}