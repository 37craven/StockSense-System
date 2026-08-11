using Microsoft.AspNetCore.Identity;

namespace StockSense.Web.Helpers;

public static class IdentityErrorFeedback
{
    public static string GetUserMessage(IEnumerable<IdentityError> errors)
        => string.Join(Environment.NewLine, GetUserMessages(errors));

    public static IReadOnlyList<string> GetUserMessages(IEnumerable<IdentityError> errors)
    {
        var errorList = errors.ToList();

        if (errorList.Any(error =>
                error.Code is nameof(IdentityErrorDescriber.DuplicateEmail)
                    or nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            return ["An account with this email address already exists. Try logging in or use a different email address."];
        }

        if (errorList.Any(error =>
                error.Code is nameof(IdentityErrorDescriber.InvalidEmail)
                    or nameof(IdentityErrorDescriber.InvalidUserName)))
        {
            return ["Enter a valid email address."];
        }

        var passwordMessages = errorList
            .Where(error => error.Code.StartsWith("Password", StringComparison.Ordinal))
            .Select(error => error.Description)
            .Distinct()
            .ToList();

        if (passwordMessages.Count > 0)
        {
            return passwordMessages;
        }

        return ["We couldn't create the account. Please review the information and try again."];
    }
}
