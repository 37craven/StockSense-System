namespace StockSense.Tests;

public sealed class AppointmentAssetLoadingTests
{
    [Fact]
    public void Appointment_DoesNotEagerlyLoadPosScannerAssets()
    {
        var root = FindRepositoryRoot();
        var app = File.ReadAllText(Path.Combine(
            root, "StockSense.Web", "Components", "App.razor"));
        var appointment = File.ReadAllText(Path.Combine(
            root, "StockSense.Client", "Pages", "CustomerBookingAppointment.razor"));
        var loader = File.ReadAllText(Path.Combine(
            root, "StockSense.Web", "wwwroot", "js", "barcodeScannerLoader.js"));
        var scanner = File.ReadAllText(Path.Combine(
            root, "StockSense.Web", "wwwroot", "js", "barcodeScanner.js"));

        Assert.DoesNotContain("<script src=\"https://unpkg.com/html5-qrcode", app, StringComparison.Ordinal);
        Assert.DoesNotContain("<script src=\"js/barcodeScanner.js", app, StringComparison.Ordinal);
        Assert.DoesNotContain("<script src=\"/js/barcodeScanner.js", app, StringComparison.Ordinal);
        Assert.Contains("<script src=\"/js/barcodeScannerLoader.js", app, StringComparison.Ordinal);
        Assert.Contains("document.createElement(\"script\")", loader, StringComparison.Ordinal);
        Assert.Contains("html5QrcodeScriptUrl", scanner, StringComparison.Ordinal);
        Assert.Contains("await ensureHtml5QrcodeLoaded();", scanner, StringComparison.Ordinal);

        // Keep this route off the 13 MB WebAssembly startup path while preserving browser cookie API calls.
        Assert.Contains("@rendermode InteractiveServer", appointment, StringComparison.Ordinal);
        Assert.Contains("stockSenseApi.readJsonTransfer", appointment, StringComparison.Ordinal);
        Assert.Contains("const int chunkSize = 16 * 1024", appointment, StringComparison.Ordinal);
        Assert.Contains("<BbToastProvider Position=", appointment, StringComparison.Ordinal);
        Assert.Contains("stockSenseApi.beginJsonTransfer", appointment, StringComparison.Ordinal);
        Assert.Contains("stockSenseApi.postJson", appointment, StringComparison.Ordinal);
        Assert.DoesNotContain("@inject HttpClient Http", appointment, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveWebAssembly", appointment, StringComparison.Ordinal);
        Assert.DoesNotContain("isLoading ? \"hidden\"", appointment, StringComparison.Ordinal);
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
