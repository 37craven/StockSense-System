using StockSense.Client.Components;

namespace StockSense.Tests;

public sealed class AssistanceSuggestionsTests
{
    [Fact]
    public void Customer_suggestions_cover_customer_tasks_without_internal_operations()
    {
        var suggestions = AssistanceSuggestions.ForRole("Customer");
        var labels = suggestions.Select(value => value.Label).ToList();

        Assert.Equal(5, suggestions.Count);
        Assert.Contains("Product availability", labels);
        Assert.Contains("Services offered", labels);
        Assert.Contains("Appointment availability", labels);
        Assert.Contains("Build quotations", labels);
        Assert.Contains("Compatibility", labels);
        Assert.DoesNotContain(suggestions, value => value.Prompt.Contains("supplier", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(suggestions, value => value.Prompt.Contains("sales summary", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Admin_suggestions_cover_management_operations()
    {
        var prompts = AssistanceSuggestions.ForRole("Admin").Select(value => value.Prompt).ToList();

        Assert.Contains(prompts, value => value.Contains("sales", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(prompts, value => value.Contains("automatic-order", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(prompts, value => value.Contains("supplier", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(prompts, value => value.Contains("user", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Employee_suggestions_stay_within_operational_tasks()
    {
        var prompts = AssistanceSuggestions.ForRole("Employee").Select(value => value.Prompt).ToList();

        Assert.Contains(prompts, value => value.Contains("low or out of stock", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(prompts, value => value.Contains("appointment", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(prompts, value => value.Contains("build", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(prompts, value => value.Contains("compatible", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(prompts, value => value.Contains("supplier", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(prompts, value => value.Contains("sales summary", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Unknown_role_falls_back_to_customer_safe_suggestions()
    {
        Assert.Equal(AssistanceSuggestions.ForRole("Customer"), AssistanceSuggestions.ForRole(null));
    }

    [Theory]
    [InlineData("Admin", "StockSense Management Assistant", "latest recorded system data")]
    [InlineData("Employee", "StockSense Operations Assistant", "latest recorded system data")]
    [InlineData("Customer", "Sap Shop Assistant", "Stock availability may change")]
    public void Role_presentation_uses_role_specific_safe_copy(
        string role,
        string assistantName,
        string helperText)
    {
        var copy = AssistanceRolePresentation.ForRole(role);

        Assert.Equal(assistantName, copy.AssistantName);
        Assert.Contains(helperText, copy.HelperText, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(copy.Heading));
        Assert.False(string.IsNullOrWhiteSpace(copy.Description));
        Assert.False(string.IsNullOrWhiteSpace(copy.Placeholder));
    }

    [Fact]
    public void Unknown_role_uses_customer_presentation()
    {
        Assert.Equal(
            AssistanceRolePresentation.ForRole("Customer"),
            AssistanceRolePresentation.ForRole(null));
    }
}
