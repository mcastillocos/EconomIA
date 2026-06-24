using System.Text.Json;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EconomIA.Infrastructure.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(key);

        if (value.IsNullOrEmpty)
        {
            _logger.LogDebug("Cache MISS for key: {Key}", key);
            return default;
        }

        _logger.LogDebug("Cache HIT for key: {Key}", key);
        return JsonSerializer.Deserialize<T>((string)value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(10));
        _logger.LogDebug("Cache SET for key: {Key}", key);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);
        _logger.LogDebug("Cache REMOVE for key: {Key}", key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync(key);
    }
}
