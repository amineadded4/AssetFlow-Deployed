// ============================================================
// AssetFlow.Application / Interfaces / ICommandeService.cs — v4
// Ajout : ModifierAsync, GetLignesMaterielsAsync
// ============================================================

using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface ICommandeService
    {
        Task<IEnumerable<CommandeDto>>              GetAllAsync();
        Task<IEnumerable<CommandeDto>>              GetByMaterielAsync(int materielId);
        Task<CommandeDto?>                          GetByIdAsync(int id);

        /// <summary>UNE LIGNE PAR MATERIEL avec ses commandes imbriquées</summary>
        Task<IEnumerable<LigneMaterielDto>>         GetLignesMaterielsAsync();

        /// <summary>Compatibilité : une ligne par commande</summary>
        Task<IEnumerable<LigneCommandeMaterielDto>> GetLignesCommandesAsync();

        Task<IEnumerable<ArticleDto>>               GetArticlesByMaterielAsync(int materielId);
        Task<IEnumerable<ArticleDto>>               GetArticlesByCommandeAsync(int commandeId);

        Task<CommandeReponseDto> CreerAsync(CreerCommandeDto dto);

        /// <summary>Modifier N°commande, fournisseur, dates (sans changer quantité)</summary>
        Task<CommandeReponseDto> ModifierAsync(ModifierCommandeDto dto);

        Task<CommandeReponseDto> SupprimerAsync(int id);
    }
}