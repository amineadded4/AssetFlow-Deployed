using AssetFlow.Application.Interfaces;
using StackExchange.Redis;

namespace AssetFlow.Infrastructure.Services
{
    public class RedisOffreService : IRedisOffreService
    {
        private readonly IDatabase _db;

        public RedisOffreService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task SaveOffreSelectionAsync(string key, string jsonValue, TimeSpan? expiry = null)
        {
            await _db.StringSetAsync(key, jsonValue, expiry ?? TimeSpan.FromDays(7));
        }

        public async Task<string?> GetOffreSelectionAsync(string key)
        {
            var val = await _db.StringGetAsync(key);
            return val.IsNullOrEmpty ? null : val.ToString();
        }

        public async Task DeleteOffreSelectionAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }
    }
}