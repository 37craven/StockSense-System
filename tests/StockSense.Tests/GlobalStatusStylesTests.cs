namespace StockSense.Tests;

public sealed class GlobalStatusStylesTests
{
    [Fact]
    public void AppStyles_ContainProductStatusStates()
    {
        var repositoryRoot = FindRepositoryRoot();
        var css = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Web",
            "wwwroot",
            "styles",
            "app.css"));

        Assert.Contains(".ss-product-status.ss-status-active", css, StringComparison.Ordinal);
        Assert.Contains("border-color: #86efac !important", css, StringComparison.Ordinal);
        Assert.Contains("background-color: #dcfce7 !important", css, StringComparison.Ordinal);
        Assert.Contains("color: #166534 !important", css, StringComparison.Ordinal);
        Assert.Contains(".ss-product-status.ss-status-inactive", css, StringComparison.Ordinal);
        Assert.Contains("background-color: #e5e7eb !important", css, StringComparison.Ordinal);

        var component = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Client",
            "Pages",
            "Admin",
            "ManageSafetyStock.razor"));
        Assert.Contains("ss-product-status", component, StringComparison.Ordinal);
        Assert.Contains("ss-status-active", component, StringComparison.Ordinal);
        Assert.Contains("ss-status-inactive", component, StringComparison.Ordinal);

        var appShell = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Web",
            "Components",
            "App.razor"));
        Assert.Contains("styles/app.css?v=", appShell, StringComparison.Ordinal);
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
