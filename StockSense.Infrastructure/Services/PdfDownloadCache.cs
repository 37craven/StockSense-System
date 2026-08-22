using System.Collections.Concurrent;
namespace StockSense.Infrastructure.Services;

public class PdfDownloadCache
{
    private static readonly ConcurrentDictionary<string, (byte[] Data, DateTime ExpiresAt)> Cache = new();
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    public string Store(byte[] data)
    {
        var token = Guid.NewGuid().ToString("N");
        Cache[token] = (data, DateTime.UtcNow.Add(Ttl));
        return token;
    }

    public byte[]? Retrieve(string token)
    {
        if (Cache.TryRemove(token, out var entry))
        {
            if (DateTime.UtcNow <= entry.ExpiresAt) return entry.Data;
            // expired – treat as miss
            return null;
        }
        return null;
    }

    /// <summary>
    /// Minimal expiration support: removes entries older than TTL. Call periodically or rely on Retrieve expiry check.
    /// Keeps static ConcurrentDictionary but prevents unbounded growth if token is never retrieved.
    /// </summary>
    public static int EvictExpired()
    {
        var now = DateTime.UtcNow;
        var expired = Cache.Where(kv => kv.Value.ExpiresAt <= now).Select(kv => kv.Key).ToList();
        var removed = 0;
        foreach (var key in expired)
            if (Cache.TryRemove(key, out _)) removed++;
        return removed;
    }
}
