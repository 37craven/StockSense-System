using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace StockSense.Tests;

public sealed class LogoutAntiforgeryTests
{
    [Fact]
    public async Task Server_issued_form_token_and_cookie_validate_for_logout_post()
    {
        using var services = CreateServices();
        var antiforgery = services.GetRequiredService<IAntiforgery>();
        var issueContext = CreateContext(services);
        var tokens = antiforgery.GetAndStoreTokens(issueContext);
        var cookie = issueContext.Response.Headers.SetCookie.ToString().Split(';')[0];

        var postContext = CreateContext(services);
        postContext.Request.Method = HttpMethods.Post;
        postContext.Request.Headers.Cookie = cookie;
        postContext.Request.ContentType = "application/x-www-form-urlencoded";
        postContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            [tokens.FormFieldName] = tokens.RequestToken
        });

        await antiforgery.ValidateRequestAsync(postContext);
    }

    [Fact]
    public async Task Logout_post_without_server_token_is_rejected()
    {
        using var services = CreateServices();
        var antiforgery = services.GetRequiredService<IAntiforgery>();
        var context = CreateContext(services);
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Form = new FormCollection([]);

        await Assert.ThrowsAsync<AntiforgeryValidationException>(
            () => antiforgery.ValidateRequestAsync(context));
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        services.AddAntiforgery();
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateContext(IServiceProvider services) => new()
    {
        RequestServices = services
    };
}
