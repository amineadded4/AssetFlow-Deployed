// ============================================================
// AssetFlow.Application / Interfaces / ICommandeService.cs
// ============================================================

using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface ICommandeService
    {
        Task<IEnumerable<CommandeDto>> GetAllAsync();
        Task<IEnumerable<CommandeDto>> GetByMaterielAsync(int materielId);
        Task<CommandeDto?> GetByIdAsync(int id);

        /// <summary>
        /// Crée la commande + génère les ArticleIndividuel + 
        /// met à jour QuantiteStock du Materiel
        /// </summary>
        Task<CommandeReponseDto> CreerAsync(CreerCommandeDto dto);
        Task<CommandeReponseDto> SupprimerAsync(int id);

        /// <summary>Vue enrichie pour le tableau Matériel</summary>
        Task<IEnumerable<MaterielAvecCommandeDto>> GetMaterielsAvecDerniereCommandeAsync();

        /// <summary>Articles individuels d'un matériel</summary>
        Task<IEnumerable<ArticleDto>> GetArticlesByMaterielAsync(int materielId);
    }
}