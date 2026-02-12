using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RAG.Domain.Interfaces;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RAG.Infrastructure.Caching;

public class SemanticCacheService : ISemanticCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SemanticCacheService> _logger;
    private readonly TimeSpan _defaultExpiration;

    public SemanticCacheService(
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<SemanticCacheService> logger)
    {
        _redis = redis;
        _logger = logger;

        var ttlHours = configuration.GetValue<int>("Cache:TtlHours", 24);
        _defaultExpiration = TimeSpan.FromHours(ttlHours);
    }

    public async Task<string?> GetSimilarResponseAsync(
        string query,
        float similarityThreshold = 0.92f,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();

            // For simple implementation, use hash of query
            // In production, would use vector similarity search with Redis Stack
            var queryHash = ComputeHash(query.ToLowerInvariant().Trim());
            var cacheKey = $"semantic:{queryHash}";

            var cached = await db.StringGetAsync(cacheKey);

            if (!cached.HasValue)
            {
                _logger.LogDebug("Semantic cache miss for query hash: {Hash}", queryHash);
                return null;
            }

            var cachedData = JsonSerializer.Deserialize<CachedResponse>(cached.ToString());

            if (cachedData == null)
                return null;

            // Refresh TTL on cache hit
            await db.KeyExpireAsync(cacheKey, _defaultExpiration);

            _logger.LogInformation("Semantic cache hit for query: {Query}", query);
            return cachedData.Response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving semantic cache");
            return null;
        }
    }

    public async Task CacheResponseAsync(
        string query,
        string response,
        float[] embedding,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var queryHash = ComputeHash(query.ToLowerInvariant().Trim());
            var cacheKey = $"semantic:{queryHash}";

            var cacheData = new CachedResponse
            {
                Query = query,
                Response = response,
                Timestamp = DateTime.UtcNow,
                Embedding = embedding
            };

            var serialized = JsonSerializer.Serialize(cacheData);
            await db.StringSetAsync(cacheKey, serialized, expiration ?? _defaultExpiration);

            _logger.LogDebug("Cached response for query: {Query}", query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching semantic response");
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16]; // Use first 16 chars
    }

    private class CachedResponse
    {
        public string Query { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
