using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface IStatistiquesITService
    {
        Task<DashboardITStatsDto> GetDashboardITAsync();
    }
}
