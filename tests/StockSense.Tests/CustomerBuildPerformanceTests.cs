namespace StockSense.Tests;

public sealed class CustomerBuildPerformanceTests
{
    [Fact]
    public void BuildRoutes_AvoidWebAssemblyStartupAndUseBrowserAuthenticatedApiCalls()
    {
        var root = FindRepositoryRoot();
        var build = File.ReadAllText(Path.Combine(
            root, "StockSense.Client", "Pages", "CustomerMotorBuild.razor"));
        var records = File.ReadAllText(Path.Combine(
            root, "StockSense.Client", "Pages", "CustomerBuildRecord.razor"));
        var app = File.ReadAllText(Path.Combine(
            root, "StockSense.Web", "Components", "App.razor"));
        var program = File.ReadAllText(Path.Combine(
            root, "StockSense.Web", "Program.cs"));

        foreach (var page in new[] { build, records })
        {
            Assert.Contains("@rendermode InteractiveServer", page, StringComparison.Ordinal);
            Assert.DoesNotContain("InteractiveWebAssembly", page, StringComparison.Ordinal);
            Assert.DoesNotContain("@inject HttpClient Http", page, StringComparison.Ordinal);
        }

        Assert.Contains("stockSenseApi.beginJsonTransfer", build, StringComparison.Ordinal);
        Assert.Contains("stockSenseApi.postJson", build, StringComparison.Ordinal);
        Assert.Contains("stockSenseApi.get", records, StringComparison.Ordinal);
        Assert.Contains("stockSenseApi.put", records, StringComparison.Ordinal);
        Assert.Contains("get: async function(url)", app, StringComparison.Ordinal);
        Assert.Contains("beginJsonTransfer: async function(url)", app, StringComparison.Ordinal);
        Assert.Contains("stockSenseApi.readJsonTransfer", build, StringComparison.Ordinal);
        Assert.Contains("const int chunkSize = 16 * 1024", build, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.Delay(200)", build, StringComparison.Ordinal);
        Assert.Contains("MaximumReceiveMessageSize = 2 * 1024 * 1024", program, StringComparison.Ordinal);
        Assert.Contains("@if (showDetailModal)", records, StringComparison.Ordinal);
        Assert.Contains("<script src=\"_framework/blazor.web.js\" defer>", app, StringComparison.Ordinal);
        Assert.Contains("barcodeScannerLoader.js?v=20260819-fullres-3\" defer", app, StringComparison.Ordinal);
        Assert.Contains("passwordVisibility.js?v=20260810-password-toggle-2\" defer", app, StringComparison.Ordinal);
        Assert.Contains("private const int ProductPageSize = 12", build, StringComparison.Ordinal);
        Assert.Contains("@foreach (var product in VisibleProducts)", build, StringComparison.Ordinal);
        Assert.Contains("content-visibility: auto", build, StringComparison.Ordinal);
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
