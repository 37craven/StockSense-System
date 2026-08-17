namespace StockSense.Tests;

public sealed class LoginDarkModeContrastTests
{
    [Fact]
    public void LoginLinks_UseHighContrastForegroundInDarkMode()
    {
        var repositoryRoot = FindRepositoryRoot();
        var loginPage = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Web",
            "Components",
            "Account",
            "Pages",
            "Login.razor"));
        var appStyles = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Web",
            "wwwroot",
            "styles",
            "app.css"));

        Assert.Equal(2, CountOccurrences(loginPage, "class=\"login-action-link\""));
        Assert.Contains("Forgot password?</a>", loginPage, StringComparison.Ordinal);
        Assert.Contains("Register as a new user</a>", loginPage, StringComparison.Ordinal);
        Assert.Contains(".login-action-link {", appStyles, StringComparison.Ordinal);
        Assert.Contains("color: var(--primary);", appStyles, StringComparison.Ordinal);
        Assert.Contains(".dark .login-action-link { color: var(--foreground); }", appStyles, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var offset = 0;

        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
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
