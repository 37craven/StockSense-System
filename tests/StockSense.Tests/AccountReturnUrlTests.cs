using StockSense.Web.Components.Account;

namespace StockSense.Tests;

public sealed class AccountReturnUrlTests
{
    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("admin/dashboard", "/admin/dashboard")]
    [InlineData("/assistance?tab=open", "/assistance?tab=open")]
    [InlineData("~/Account/Manage", "/Account/Manage")]
    [InlineData("https://evil.example", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData("/\\evil.example", "/")]
    public void Normalize_allows_only_local_return_paths(string? value, string expected)
    {
        Assert.Equal(expected, AccountReturnUrl.Normalize(value));
    }
}
