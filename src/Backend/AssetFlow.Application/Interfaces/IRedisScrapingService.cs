// src/Backend/AssetFlow.Application/Interfaces/IRedisScrapingService.cs
namespace AssetFlow.Application.Interfaces;

public interface IRedisScrapingService
{
    Task SaveResultatAsync(string jsonValue);
    Task<string?> GetResultatAsync();
    Task DeleteResultatAsync();
}