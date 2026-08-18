namespace StockSense.Tests;

public sealed class RegisterDarkModeContrastTests
{
    [Fact]
    public void RegisterLoginLink_UsesHighContrastForegroundInDarkMode()
    {
        var repositoryRoot = FindRepositoryRoot();
        var registerPage = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Web",
            "Components",
            "Account",
            "Pages",
            "Register.razor"));
        var appStyles = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Web",
            "wwwroot",
            "styles",
            "app.css"));

        Assert.Contains("class=\"register-login-link\"", registerPage, StringComparison.Ordinal);
        Assert.Contains(".register-login-link {", appStyles, StringComparison.Ordinal);
        Assert.Contains("color: var(--primary);", appStyles, StringComparison.Ordinal);
        Assert.Contains(".dark .register-login-link { color: var(--foreground); }", appStyles, StringComparison.Ordinal);
        Assert.Contains("text-decoration: underline;", appStyles, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "StockSense.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the StockSense repository root.");
    }
}
