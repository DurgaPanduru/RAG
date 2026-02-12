namespace RAG.Domain.Interfaces;

/// <summary>
/// Interface for caching operations
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for semantic caching with similarity search
/// </summary>
public interface ISemanticCacheService
{
    Task<string?> GetSimilarResponseAsync(string query, float similarityThreshold = 0.92f, CancellationToken cancellationToken = default);
    Task CacheResponseAsync(string query, string response, float[] embedding, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
}
