using System.Collections.Concurrent;
namespace StockSense.Infrastructure.Services;

public class PdfDownloadCache
{
    private static readonly ConcurrentDictionary<string, byte[]> Cache = new();

    public string Store(byte[] data)
    {
        var token = Guid.NewGuid().ToString("N");
        Cache[token] = data;
        return token;
    }

    public byte[]? Retrieve(string token)
    {
        Cache.TryRemove(token, out var data);
        return data;
    }
}
