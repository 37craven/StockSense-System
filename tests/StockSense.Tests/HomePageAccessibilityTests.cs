namespace StockSense.Tests;

public sealed class HomePageAccessibilityTests
{
    [Fact]
    public void HomePage_UsesHighContrastAboutTextAndScopedTouchTargets()
    {
        var repositoryRoot = FindRepositoryRoot();
        var homePage = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Client",
            "Pages",
            "Home.razor"));
        var publicNav = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Client",
            "Layout",
            "PublicNav.razor"));
        var appStyles = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Web",
            "wwwroot",
            "styles",
            "app.css"));
        var aboutStart = homePage.IndexOf("<section class=\"about\"", StringComparison.Ordinal);
        var aboutEnd = homePage.IndexOf("<section class=\"cta\"", aboutStart, StringComparison.Ordinal);
        var aboutMarkup = homePage[aboutStart..aboutEnd];

        Assert.DoesNotContain("color: var(--muted-foreground)", aboutMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("color: var(--primary)", aboutMarkup, StringComparison.Ordinal);
        Assert.Contains("Class=\"homepage-touch-target\"", homePage, StringComparison.Ordinal);
        Assert.Contains("<div class=\"home-footer\">", homePage, StringComparison.Ordinal);
        Assert.Contains("home-public-nav", publicNav, StringComparison.Ordinal);
        Assert.Contains(".home .homepage-touch-target", appStyles, StringComparison.Ordinal);
        Assert.Contains(".home-footer a", appStyles, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem;", appStyles, StringComparison.Ordinal);
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
