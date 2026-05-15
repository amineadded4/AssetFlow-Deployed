// src/Backend/AssetFlow.Infrastructure/Services/RedisScrapingService.cs
using AssetFlow.Application.Interfaces;
using StackExchange.Redis;

namespace AssetFlow.Infrastructure.Services;

public class RedisScrapingService : IRedisScrapingService
{
    private readonly IDatabase _db;
    private const string KEY_PREFIX = "scraping:user:";

    public RedisScrapingService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task SaveResultatAsync(string jsonValue, string userId = "global")
    {
        await _db.StringSetAsync($"{KEY_PREFIX}{userId}", jsonValue, TimeSpan.FromDays(1));
    }

    public async Task<string?> GetResultatAsync(string userId = "global")
    {
        var val = await _db.StringGetAsync($"{KEY_PREFIX}{userId}");
        return val.IsNullOrEmpty ? null : val.ToString();
    }

    public async Task DeleteResultatAsync(string userId = "global")
    {
        await _db.KeyDeleteAsync($"{KEY_PREFIX}{userId}");
    }
}