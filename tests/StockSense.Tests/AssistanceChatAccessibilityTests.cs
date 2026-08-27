namespace StockSense.Tests;

public sealed class AssistanceChatAccessibilityTests
{
    [Fact]
    public void DecorativeIcons_HaveAriaHidden()
    {
        var repositoryRoot = FindRepositoryRoot();
        var component = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "StockSense.Client",
            "Components",
            "AssistanceChat.razor"));

        Assert.Contains("Name=\"bot\" Size=\"28\" aria-hidden=\"true\"", component, StringComparison.Ordinal);
        Assert.Contains("Name=\"@suggestion.Icon\" Size=\"15\" aria-hidden=\"true\"", component, StringComparison.Ordinal);
        Assert.Contains("Name=\"arrow-up-right\" Size=\"14\" aria-hidden=\"true\"", component, StringComparison.Ordinal);
        Assert.Contains("Name=\"plus\" Size=\"15\" aria-hidden=\"true\"", component, StringComparison.Ordinal);
        Assert.Contains("Name=\"circle-alert\" Size=\"15\" aria-hidden=\"true\"", component, StringComparison.Ordinal);
        Assert.Contains("Name=\"arrow-up\" Size=\"16\" aria-hidden=\"true\"", component, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerAssistance_AvoidsWebAssemblyStartupAndUsesAuthenticatedBrowserFetch()
    {
        var repositoryRoot = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(
            repositoryRoot, "StockSense.Client", "Pages", "Assistance.razor"));
        var component = File.ReadAllText(Path.Combine(
            repositoryRoot, "StockSense.Client", "Components", "AssistanceChat.razor"));

        Assert.Contains("@rendermode InteractiveServer", page, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveWebAssembly", page, StringComparison.Ordinal);
        Assert.Contains("<AssistanceChat />", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowInitialLoading=\"true\"", page, StringComparison.Ordinal);
        Assert.Contains("stockSenseApi.postJson", component, StringComparison.Ordinal);
        Assert.DoesNotContain("@inject HttpClient Http", component, StringComparison.Ordinal);
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
