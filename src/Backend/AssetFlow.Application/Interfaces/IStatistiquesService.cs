using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IStatistiquesService
    {
        Task<DashboardStatsDto> GetDashboardAsync(int annee, int? moisDebut, int? moisFin);
    }
}
