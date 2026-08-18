using Microsoft.Net.Http.Headers;

namespace StockSense.Web.Utility.Security;

public static class SecurityHeaderPolicy
{
    private const string ContentSecurityPolicyValue =
        "default-src 'self';" +
        " script-src 'self' 'wasm-unsafe-eval';" +
        " style-src 'self' 'unsafe-inline';" +
        " img-src 'self' data:;" +
        " font-src 'self' data:;" +
        " connect-src 'self' ws: wss:;" +
        " object-src 'none';" +
        " base-uri 'self';" +
        " frame-ancestors 'self';" +
        " form-action 'self'";

    // ponytail: camera=(self) keeps the POS barcode scanner (getUserMedia) working;
    // everything else is denied to third-party origins.
    private const string PermissionsPolicyValue =
        "camera=(self), geolocation=(), microphone=(), payment=(), usb=()";

    public static void Apply(HttpResponse response)
    {
        response.Headers[HeaderNames.ContentSecurityPolicy] = ContentSecurityPolicyValue;
        response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        response.Headers["Permissions-Policy"] = PermissionsPolicyValue;
    }
}