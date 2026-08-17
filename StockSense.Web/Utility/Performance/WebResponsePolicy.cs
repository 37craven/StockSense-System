using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace StockSense.Web.Utility.Performance;

public static class WebResponsePolicy
{
    private const string ImmutableCacheControl = "public,max-age=31536000,immutable";
    private const string StaticCacheControl = "public,max-age=604800";
    private const string NoStoreCacheControl = "no-store";

    private static readonly string[] SensitivePathPrefixes =
    [
        "/api",
        "/Account",
        "/Identity"
    ];

    public static string GetStaticAssetCacheControl(PathString path, QueryString queryString)
    {
        if (IsRuntimeConfiguration(path))
            return NoStoreCacheControl;

        return IsVersionedAsset(path, queryString)
            ? ImmutableCacheControl
            : StaticCacheControl;
    }

    public static bool ShouldPreventCaching(HttpRequest request, HttpResponse response)
    {
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
            return true;

        if (SensitivePathPrefixes.Any(prefix =>
                request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase)))
            return true;

        var contentType = response.ContentType;
        return contentType is not null
            && (contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)
                || contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)
                || contentType.StartsWith("application/problem+json", StringComparison.OrdinalIgnoreCase));
    }

    public static void PreventCaching(HttpResponse response)
    {
        response.Headers[HeaderNames.CacheControl] = NoStoreCacheControl;
        response.Headers.Remove(HeaderNames.Expires);
        response.Headers.Remove(HeaderNames.Pragma);
    }

    private static bool IsRuntimeConfiguration(PathString path)
    {
        var fileName = Path.GetFileName(path.Value ?? string.Empty);
        if (fileName.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)
            && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return true;

        return fileName.Equals("blazor.boot.json", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("service-worker.js", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("service-worker-assets.js", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVersionedAsset(PathString path, QueryString queryString)
    {
        if (queryString.HasValue && QueryHelpers.ParseQuery(queryString.Value!).ContainsKey("v"))
            return true;

        var fileName = Path.GetFileNameWithoutExtension(path.Value ?? string.Empty);
        return fileName.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment.Length >= 8 && segment.All(Uri.IsHexDigit));
    }
}
