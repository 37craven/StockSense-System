namespace StockSense.Tests;

public sealed class CustomerBookingAppointmentAccessibilityTests
{
    [Fact]
    public void BookingPage_HidesScreenshotReportedDecorativeIcons()
    {
        var component = ReadBookingComponent();

        Assert.Contains("aria-hidden=\"true\"><LucideIcon Name=\"shopping-cart\"", component, StringComparison.Ordinal);
        Assert.Contains("aria-hidden=\"true\"><LucideIcon Name=\"arrow-left\"", component, StringComparison.Ordinal);
        Assert.Contains("aria-hidden=\"true\"><LucideIcon Name=\"arrow-right\"", component, StringComparison.Ordinal);
        Assert.Contains("aria-hidden=\"true\"><LucideIcon Name=\"calendar-days\"", component, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingPage_UsesContrastingPriceAndServiceFooterColors()
    {
        var component = ReadBookingComponent();

        Assert.Contains("text-xl font-black text-foreground tracking-tight", component, StringComparison.Ordinal);
        Assert.Contains("service-management-card__footer", component, StringComparison.Ordinal);
        Assert.Contains("service-management-card__title", component, StringComparison.Ordinal);
        Assert.Contains("service-management-card__category", component, StringComparison.Ordinal);
        Assert.Contains("₱@service.TotalServicePrice.ToString(\"N0\")", component, StringComparison.Ordinal);
        Assert.DoesNotContain("background-color: color-mix(in oklch, var(--card) 80%, var(--muted))", component, StringComparison.Ordinal);
    }

    private static string ReadBookingComponent()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "StockSense.slnx")))
            {
                return File.ReadAllText(Path.Combine(
                    directory.FullName,
                    "StockSense.Client",
                    "Pages",
                    "CustomerBookingAppointment.razor"));
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the StockSense repository root.");
    }
}
