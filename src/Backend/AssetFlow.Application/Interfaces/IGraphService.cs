using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IGraphService
    {
        // Legacy
        Task<GraphResponseDto> GetGraphAsync();
        Task<GraphInsightDto?> GetInsightForNodeAsync(string nodeId);

        // Stats
        Task<GraphStatsDto> GetStatsAsync();

        // Listes panneau gauche
        Task<List<GraphEntitySummaryDto>> GetMaterielsAsync();
        Task<List<GraphEntitySummaryDto>> GetUtilisateursAsync();
        Task<List<GraphEntitySummaryDto>> GetDemandesAsync();
        Task<List<GraphEntitySummaryDto>> GetProjetsAsync();

        // Graphes contextuels
        Task<GraphResponseDto> GetGraphForMaterielAsync(int materielId);
        Task<GraphResponseDto> GetGraphForUtilisateurAsync(int userId);
        Task<GraphResponseDto> GetGraphForDemandeAsync(int demandeId);
        Task<GraphResponseDto> GetGraphForProjetAsync(int projetId);
    }
}