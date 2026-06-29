using System.Text.Json;
using EconomIA.Domain.Ports;
using EconomIA.Infrastructure.Telemetry;
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
        try
        {
            if (!_redis.IsConnected) return default;
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                OpenTelemetryConfig.CacheMisses.Add(1, new KeyValuePair<string, object?>("cache.key", key));
                return default;
            }

            OpenTelemetryConfig.CacheHits.Add(1, new KeyValuePair<string, object?>("cache.key", key));
            return JsonSerializer.Deserialize<T>((string)value!);
        }
        catch (RedisConnectionException)
        {
            _logger.LogDebug("Redis no disponible, cache MISS para {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            if (!_redis.IsConnected) return;
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(10));
        }
        catch (RedisConnectionException)
        {
            _logger.LogDebug("Redis no disponible, cache SET ignorado para {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            if (!_redis.IsConnected) return;
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (RedisConnectionException)
        {
            _logger.LogDebug("Redis no disponible, cache REMOVE ignorado para {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            if (!_redis.IsConnected) return false;
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        catch (RedisConnectionException)
        {
            return false;
        }
    }
}
