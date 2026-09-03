namespace StockSense.Web.Components.Account;

public static class AccountReturnUrl
{
    public static string Normalize(string? returnUrl)
    {
        var value = returnUrl?.Trim();
        if (string.IsNullOrEmpty(value)
            || value.Contains('\\')
            || value.Any(char.IsControl)
            || (Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri) && absoluteUri.Scheme is "http" or "https")
            || value.StartsWith("//", StringComparison.Ordinal))
        {
            return "/";
        }

        if (value.StartsWith("~/", StringComparison.Ordinal)) value = value[1..];
        return value.StartsWith("/", StringComparison.Ordinal) ? value : $"/{value}";
    }
}
