using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using StockSense.Web.Utility.Performance;

namespace StockSense.Tests;

public sealed class WebResponsePolicyTests
{
    [Theory]
    [InlineData("/styles/app.css", "?v=20260812", "public,max-age=31536000,immutable")]
    [InlineData("/_framework/runtime.0123456789abcdef.js", "", "public,max-age=31536000,immutable")]
    [InlineData("/styles/tailwind.css", "", "public,max-age=604800")]
    [InlineData("/appsettings.json", "", "no-store")]
    [InlineData("/appsettings.Production.json", "?v=1", "no-store")]
    [InlineData("/_framework/blazor.boot.json", "", "no-store")]
    [InlineData("/service-worker.js", "", "no-store")]
    public void StaticAssetPolicy_UsesSafeCacheLifetime(string path, string query, string expected)
    {
        var result = WebResponsePolicy.GetStaticAssetCacheControl(
            new PathString(path),
            new QueryString(query));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("GET", "/appointment", "text/html; charset=utf-8")]
    [InlineData("GET", "/api/appointments", "application/json; charset=utf-8")]
    [InlineData("GET", "/Account/Login", "text/plain")]
    [InlineData("POST", "/appointment", "text/plain")]
    public void DynamicPolicy_PreventsCachingSensitiveResponses(
        string method,
        string path,
        string contentType)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.ContentType = contentType;

        Assert.True(WebResponsePolicy.ShouldPreventCaching(context.Request, context.Response));
    }

    [Fact]
    public void DynamicPolicy_DoesNotOverridePublicStaticResponse()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/styles/app.css";
        context.Response.ContentType = "text/css";

        Assert.False(WebResponsePolicy.ShouldPreventCaching(context.Request, context.Response));
    }

    [Fact]
    public void PreventCaching_RemovesLegacyCacheHeaders()
    {
        var context = new DefaultHttpContext();
        context.Response.Headers[HeaderNames.Expires] = "tomorrow";
        context.Response.Headers[HeaderNames.Pragma] = "cache";

        WebResponsePolicy.PreventCaching(context.Response);

        Assert.Equal("no-store", context.Response.Headers[HeaderNames.CacheControl]);
        Assert.False(context.Response.Headers.ContainsKey(HeaderNames.Expires));
        Assert.False(context.Response.Headers.ContainsKey(HeaderNames.Pragma));
    }
}
