using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace SchoolOS.BuildingBlocks.Secrets;

public sealed class CachedSecretProvider(
    ISecretProvider inner,
    IMemoryCache cache,
    IOptions<SecretsOptions> options,
    SecretsTelemetry? telemetry = null)
    : ISecretProvider
{
    private const string CacheKeyPrefix = "SchoolOS.Secrets:";
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(options.Value.CacheTtlMinutes);

    public async Task<string?> GetSecretAsync(SecretKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (cache.TryGetValue(CacheKey(key), out string? cached))
        {
            telemetry?.RecordCacheLookup(hit: true);
            return cached;
        }

        telemetry?.RecordCacheLookup(hit: false);

        var value = await inner.GetSecretAsync(key, cancellationToken);
        if (value is not null)
        {
            cache.Set(CacheKey(key), value, _ttl);
        }

        return value;
    }

    public async Task SetSecretAsync(SecretKey key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        await inner.SetSecretAsync(key, value, cancellationToken);
        cache.Remove(CacheKey(key));
    }

    public async Task DeleteSecretAsync(SecretKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        await inner.DeleteSecretAsync(key, cancellationToken);
        cache.Remove(CacheKey(key));
    }

    private static string CacheKey(SecretKey key) => CacheKeyPrefix + key.Path;
}
