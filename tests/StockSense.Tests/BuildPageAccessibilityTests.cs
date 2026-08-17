namespace StockSense.Tests;

public sealed class BuildPageAccessibilityTests
{
    [Fact]
    public void ProductCards_ProvideImageAlternativesAndHighContrastText()
    {
        var repositoryRoot = FindRepositoryRoot();
        var buildPage = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Client",
            "Pages",
            "CustomerMotorBuild.razor"));

        Assert.Contains("alt=\"@GetProductImageAlt(product)\"", buildPage, StringComparison.Ordinal);
        Assert.Contains("width=\"600\" height=\"400\" loading=\"lazy\" decoding=\"async\"", buildPage, StringComparison.Ordinal);
        Assert.Contains("class=\"build-product-grid\"", buildPage, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: repeat(2, minmax(0, 1fr))", buildPage, StringComparison.Ordinal);
        Assert.Contains("return isPlaceholder ? \"\" : $\"{product.Name} product image\";", buildPage, StringComparison.Ordinal);
        Assert.Contains("imageUrl.Contains(\"placeholder\"", buildPage, StringComparison.Ordinal);
        Assert.Contains("class=\"text-xs text-foreground\">@product.Brand", buildPage, StringComparison.Ordinal);
        Assert.Contains("class=\"text-sm font-semibold text-foreground mt-1\"", buildPage, StringComparison.Ordinal);
        Assert.Contains("bg-foreground text-background px-2 py-1 text-[10px] font-extrabold", buildPage, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"text-xs text-muted-foreground\">@product.Brand", buildPage, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"text-sm font-semibold text-primary mt-1\"", buildPage, StringComparison.Ordinal);
        Assert.DoesNotContain("bg-destructive text-destructive-foreground px-2 py-1 text-[10px]", buildPage, StringComparison.Ordinal);
        Assert.Contains("font-size: var(--text-sm); font-weight: 900; color: var(--foreground);\">₱@TotalPrice", buildPage, StringComparison.Ordinal);
    }

    [Fact]
    public void DecorativeIcons_AreHiddenFromAssistiveTechnology()
    {
        var repositoryRoot = FindRepositoryRoot();
        var buildPage = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Client",
            "Pages",
            "CustomerMotorBuild.razor"));

        var iconCount = buildPage.Split("<LucideIcon", StringSplitOptions.None).Length - 1;
        var hiddenIconCount = buildPage.Split("aria-hidden=\"true\"", StringSplitOptions.None).Length - 1;

        Assert.Equal(iconCount, hiddenIconCount);
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
