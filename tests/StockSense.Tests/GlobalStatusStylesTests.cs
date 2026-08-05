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

        Assert.Contains(".product-status-badge.is-active", css, StringComparison.Ordinal);
        Assert.Contains(".product-status-toggle.is-active", css, StringComparison.Ordinal);
        Assert.Contains("background: #dcfce7", css, StringComparison.Ordinal);
        Assert.Contains(".product-status-badge.is-inactive", css, StringComparison.Ordinal);
        Assert.Contains(".product-status-toggle.is-inactive", css, StringComparison.Ordinal);
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
