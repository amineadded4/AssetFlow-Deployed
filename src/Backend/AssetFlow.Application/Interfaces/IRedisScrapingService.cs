// src/Backend/AssetFlow.Application/Interfaces/IRedisScrapingService.cs
namespace AssetFlow.Application.Interfaces;

public interface IRedisScrapingService
{
    Task SaveResultatAsync(string jsonValue, string userId = "global");
    Task<string?> GetResultatAsync(string userId = "global");
    Task DeleteResultatAsync(string userId = "global");
}