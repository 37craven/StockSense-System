namespace StockSense.Tests;

public sealed class RateLimitingConfigurationTests
{
    private static readonly string ProgramSource = File.ReadAllText(Path.Combine(
        FindRepositoryRoot(), "StockSense.Web", "Program.cs"));

    [Fact]
    public void Api_policy_is_partitioned_by_authenticated_user_then_remote_ip()
    {
        Assert.Contains("options.AddPolicy(\"api-policy\", httpContext =>", ProgramSource);
        Assert.Contains("FindFirstValue(ClaimTypes.NameIdentifier)", ProgramSource);
        Assert.Contains("httpContext.Connection.RemoteIpAddress", ProgramSource);
        Assert.Contains("PermitLimit = 300", ProgramSource);
        Assert.Contains("Window = TimeSpan.FromMinutes(1)", ProgramSource);
        Assert.Contains("QueueLimit = 10", ProgramSource);
        Assert.Contains("QueueProcessingOrder = QueueProcessingOrder.OldestFirst", ProgramSource);
        Assert.DoesNotContain("GlobalLimiter", ProgramSource);
    }

    [Fact]
    public void Rate_limiter_runs_after_auth_and_applies_only_to_controllers()
    {
        var authentication = ProgramSource.IndexOf("app.UseAuthentication();", StringComparison.Ordinal);
        var authorization = ProgramSource.IndexOf("app.UseAuthorization();", StringComparison.Ordinal);
        var rateLimiter = ProgramSource.IndexOf("app.UseRateLimiter();", StringComparison.Ordinal);

        Assert.True(authentication >= 0 && authentication < rateLimiter);
        Assert.True(authorization >= 0 && authorization < rateLimiter);
        Assert.Contains(
            "app.MapControllers().RequireRateLimiting(\"api-policy\");",
            ProgramSource);
        Assert.DoesNotContain(
            "app.MapRazorComponents<App>().RequireRateLimiting",
            ProgramSource);
        Assert.Contains(
            "app.MapAdditionalIdentityEndpoints().RequireRateLimiting(\"login-policy\")",
            ProgramSource);
    }

    [Fact]
    public void Login_policy_is_preserved()
    {
        Assert.Contains("policyName: \"login-policy\"", ProgramSource);
        Assert.Contains("opt.PermitLimit = 5", ProgramSource);
        Assert.Contains("opt.Window = TimeSpan.FromSeconds(30)", ProgramSource);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "StockSense.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the StockSense repository root.");
    }
}
