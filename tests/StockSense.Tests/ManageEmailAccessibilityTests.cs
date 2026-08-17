namespace StockSense.Tests;

public sealed class ManageEmailAccessibilityTests
{
    [Fact]
    public void EmailPage_AssociatesLabelsAndUsesReadableStatusText()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(root, "StockSense.Web", "Components", "Account", "Pages", "Manage", "Email.razor"));
        var nav = File.ReadAllText(Path.Combine(root, "StockSense.Web", "Components", "Account", "Shared", "SettingsNav.razor"));

        Assert.Contains("<label for=\"current-email\"", page, StringComparison.Ordinal);
        Assert.Contains("id=\"current-email\"", page, StringComparison.Ordinal);
        Assert.Contains("<label for=\"new-email\"", page, StringComparison.Ordinal);
        Assert.Contains("<InputText id=\"new-email\"", page, StringComparison.Ordinal);
        Assert.Contains("color: var(--foreground)", page, StringComparison.Ordinal);
        Assert.Contains("id=\"current-email\" type=\"text\" value=\"@email\" aria-label=\"Current email address\"", page, StringComparison.Ordinal);
        Assert.Contains("id=\"new-email\" @bind-Value=\"Input.NewEmail\" autocomplete=\"email\" aria-label=\"New email address\"", page, StringComparison.Ordinal);
        Assert.Contains("<span aria-hidden=\"true\"><LucideIcon Name=\"check\" Size=\"14\" /></span>", page, StringComparison.Ordinal);
        Assert.Contains("<span aria-hidden=\"true\"><LucideIcon Name=\"@link.Icon\" Size=\"16\" /></span>", nav, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "StockSense.slnx"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the StockSense repository root.");
    }
}
