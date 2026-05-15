// src/Backend/AssetFlow.Application/Interfaces/IScrapingService.cs
namespace AssetFlow.Application.Interfaces;

public interface IScrapingService
{
    Task LancerScrapingAsync(string query, string groupId, string userId);
    Task<string?> GetCachedResultAsync(string userId);
}