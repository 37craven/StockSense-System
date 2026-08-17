namespace StockSense.Web.Helpers;

/// <summary>
/// Forwards the browser's authentication cookies to outbound requests made by
/// server-rendered components, so cookie-authenticated API endpoints (e.g. api/assistance)
/// behave identically to the WASM-rendered versions of those components.
/// </summary>
public sealed class CookieForwardingHandler(IHttpContextAccessor httpContextAccessor, HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is not null && !string.IsNullOrEmpty(context.Request.Headers.Cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", context.Request.Headers.Cookie.ToString());
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
