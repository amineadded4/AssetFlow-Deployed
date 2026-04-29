// src/Backend/AssetFlow.Application/Interfaces/IScrapingService.cs
namespace AssetFlow.Application.Interfaces;

public interface IScrapingService
{
    Task LancerScrapingAsync(string query, string connectionId);
    Task<string?> GetCachedResultAsync(string query);
}