// src/Backend/AssetFlow.Infrastructure/Services/RedisScrapingService.cs
using AssetFlow.Application.Interfaces;
using StackExchange.Redis;

namespace AssetFlow.Infrastructure.Services;

public class RedisScrapingService : IRedisScrapingService
{
    private readonly IDatabase _db;
    // Clé fixe unique — toujours écrasée
    private const string KEY = "scraping:dernier_resultat";

    public RedisScrapingService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task SaveResultatAsync(string jsonValue)
    {
        // Expiry 24h + écrase l'ancienne valeur
        await _db.StringSetAsync(KEY, jsonValue, TimeSpan.FromDays(1));
    }

    public async Task<string?> GetResultatAsync()
    {
        var val = await _db.StringGetAsync(KEY);
        return val.IsNullOrEmpty ? null : val.ToString();
    }

    public async Task DeleteResultatAsync()
    {
        await _db.KeyDeleteAsync(KEY);
    }
}