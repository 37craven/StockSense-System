namespace StockSense.Tests;

public sealed class ProfileSettingsMetricsTests
{
    [Fact]
    public void PasswordFields_HaveExplicitLabels()
    {
        var page = Read("StockSense.Web", "Components", "Account", "Pages", "Manage", "ChangePassword.razor");

        Assert.Contains("<label for=\"change-current-password\"", page, StringComparison.Ordinal);
        Assert.Contains("<label for=\"change-new-password\"", page, StringComparison.Ordinal);
        Assert.Contains("<label for=\"change-confirm-password\"", page, StringComparison.Ordinal);
    }

    [Fact]
    public void PersonalData_UsesHighContrastDangerAction()
    {
        var page = Read("StockSense.Web", "Components", "Account", "Pages", "Manage", "PersonalData.razor");
        var css = Read("StockSense.Web", "wwwroot", "styles", "app.css");

        Assert.Contains("class=\"account-danger-action\"", page, StringComparison.Ordinal);
        Assert.Contains(".account-danger-action", css, StringComparison.Ordinal);
        Assert.Contains("background-color: #b91c1c", css, StringComparison.Ordinal);
        Assert.Contains("color: #ffffff", css, StringComparison.Ordinal);
    }

    [Fact]
    public void SettingsNavigation_DoesNotCreateDuplicateInteractiveCircuits()
    {
        var shell = Read("StockSense.Web", "Components", "Account", "Shared", "SettingsShell.razor");
        var nav = Read("StockSense.Web", "Components", "Account", "Shared", "SettingsNav.razor");
        var header = Read("StockSense.Web", "Components", "Account", "Shared", "SettingsPublicNav.razor");
        var app = Read("StockSense.Web", "Components", "App.razor");

        Assert.DoesNotContain("<SettingsNav @rendermode=", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("<PublicNav @rendermode=", shell, StringComparison.Ordinal);
        Assert.Contains("<SettingsPublicNav />", shell, StringComparison.Ordinal);
        Assert.Contains("<details class=\"settings-public-menu\">", header, StringComparison.Ordinal);
        Assert.Contains("data-settings-theme-toggle", header, StringComparison.Ordinal);
        Assert.Contains("@if (!IsStaticCustomerSettingsRequest)", app, StringComparison.Ordinal);
        Assert.Contains("HttpContext?.User.IsInRole(\"Customer\") == true", app, StringComparison.Ordinal);
        Assert.Contains("StartsWithSegments(\"/Account/Manage\"", app, StringComparison.Ordinal);
        Assert.DoesNotContain("@implements IDisposable", nav, StringComparison.Ordinal);
        Assert.DoesNotContain("LocationChanged +=", nav, StringComparison.Ordinal);
    }

    [Fact]
    public void MobileSettingsLinks_HaveDiscernibleNames()
    {
        var nav = Read("StockSense.Web", "Components", "Account", "Shared", "SettingsNav.razor");

        Assert.Contains("aria-label=\"@link.Label\"", nav, StringComparison.Ordinal);
        Assert.Contains("title=\"@link.Label\"", nav, StringComparison.Ordinal);
    }

    private static string Read(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { FindRepositoryRoot() }.Concat(path).ToArray()));

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
