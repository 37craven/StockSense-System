using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using StockSense.Web.Utility.Security;

namespace StockSense.Tests;

public sealed class SecurityHeaderPolicyTests
{
    [Fact]
    public void Apply_SetsContentSecurityPolicy()
    {
        var response = new DefaultHttpContext().Response;

        SecurityHeaderPolicy.Apply(response);

        var csp = response.Headers[HeaderNames.ContentSecurityPolicy].ToString();
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("object-src 'none'", csp);

        var scriptTokens = csp.Split(';')
            .First(directive => directive.TrimStart().StartsWith("script-src"))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("'self'", scriptTokens);
        Assert.Contains("'wasm-unsafe-eval'", scriptTokens);
        Assert.DoesNotContain("'unsafe-eval'", scriptTokens);
        Assert.DoesNotContain("'unsafe-inline'", scriptTokens);
    }

    [Fact]
    public void Apply_SetsNosniff()
    {
        var response = new DefaultHttpContext().Response;

        SecurityHeaderPolicy.Apply(response);

        Assert.Equal("nosniff", response.Headers[HeaderNames.XContentTypeOptions]);
    }

    [Fact]
    public void Apply_SetsStrictReferrerPolicy()
    {
        var response = new DefaultHttpContext().Response;

        SecurityHeaderPolicy.Apply(response);

        Assert.Equal("strict-origin-when-cross-origin", response.Headers["Referrer-Policy"]);
    }

    [Fact]
    public void Apply_SetsPermissionsPolicyAllowingCamera()
    {
        var response = new DefaultHttpContext().Response;

        SecurityHeaderPolicy.Apply(response);

        var permissions = response.Headers["Permissions-Policy"].ToString();
        Assert.Contains("camera=(self)", permissions);
        Assert.Contains("geolocation=()", permissions);
        Assert.Contains("unload=(self)", permissions);
        Assert.DoesNotContain("camera=*", permissions);
    }
}