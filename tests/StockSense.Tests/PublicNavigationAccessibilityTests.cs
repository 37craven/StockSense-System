namespace StockSense.Tests;

public sealed class PublicNavigationAccessibilityTests
{
    [Fact]
    public void PublicNavigation_LabelsScreenshotReportedElements()
    {
        var repositoryRoot = FindRepositoryRoot();
        var component = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Client",
            "Layout",
            "PublicNav.razor"));

        Assert.Contains("aria-label=\"SAPShop home\"", component, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"@(isDarkMode ? \"Switch to light mode\" : \"Switch to dark mode\")\"", component, StringComparison.Ordinal);
        Assert.Contains("title=\"@(isDarkMode ? \"Switch to light mode\" : \"Switch to dark mode\")\"", component, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"@(mobileMenuOpen ? \"Close navigation menu\" : \"Open navigation menu\")\"", component, StringComparison.Ordinal);
        Assert.Contains("aria-expanded=\"@(mobileMenuOpen ? \"true\" : \"false\")\"", component, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"public-mobile-menu\"", component, StringComparison.Ordinal);
        Assert.Contains("id=\"public-mobile-menu\"", component, StringComparison.Ordinal);
        Assert.Contains("<span aria-hidden=\"true\"><LucideIcon Name=\"@(mobileMenuOpen ? \"x\" : \"menu\")\"", component, StringComparison.Ordinal);
        Assert.Contains("aria-hidden=\"true\"", component, StringComparison.Ordinal);
    }

    [Fact]
    public void HomePage_HidesDecorativeIconsFromAssistiveTechnology()
    {
        var repositoryRoot = FindRepositoryRoot();
        var component = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Client",
            "Pages",
            "Home.razor"));

        Assert.Contains("<span aria-hidden=\"true\"><LucideIcon Name=\"wrench\"", component, StringComparison.Ordinal);
        Assert.Contains("<span aria-hidden=\"true\"><LucideIcon Name=\"settings\"", component, StringComparison.Ordinal);
        Assert.Contains("<span aria-hidden=\"true\"><LucideIcon Name=\"bike\"", component, StringComparison.Ordinal);
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
