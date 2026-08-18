namespace StockSense.Tests;

public sealed class CustomerAppointmentsPerformanceTests
{
    [Fact]
    public void MyBookings_AvoidsWebAssemblyStartupAndUsesBrowserAuthenticatedApiCalls()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(
            root, "StockSense.Client", "Pages", "CustomerAppointments.razor"));
        var app = File.ReadAllText(Path.Combine(
            root, "StockSense.Web", "Components", "App.razor"));

        Assert.Contains("@rendermode InteractiveServer", page, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveWebAssembly", page, StringComparison.Ordinal);
        Assert.DoesNotContain("@inject HttpClient Http", page, StringComparison.Ordinal);
        Assert.Contains("stockSenseApi.getJson", page, StringComparison.Ordinal);
        Assert.Contains("stockSenseApi.put", page, StringComparison.Ordinal);
        Assert.Contains("js/stockSenseApi.js", app, StringComparison.Ordinal);
        Assert.Contains("@if (showDetailModal)", page, StringComparison.Ordinal);
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
