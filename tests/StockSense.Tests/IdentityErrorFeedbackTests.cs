using Microsoft.AspNetCore.Identity;
using StockSense.Web.Helpers;

namespace StockSense.Tests;

public class IdentityErrorFeedbackTests
{
    [Theory]
    [InlineData("DuplicateUserName")]
    [InlineData("DuplicateEmail")]
    public void DuplicateAccountErrorsUseEmailWording(string code)
    {
        var message = IdentityErrorFeedback.GetUserMessage(
            [new IdentityError { Code = code, Description = "Username is already taken." }]);

        Assert.Contains("email address already exists", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("username", message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("InvalidUserName")]
    [InlineData("InvalidEmail")]
    public void InvalidIdentityErrorsUseEmailWording(string code)
    {
        var message = IdentityErrorFeedback.GetUserMessage(
            [new IdentityError { Code = code, Description = "Username is invalid." }]);

        Assert.Equal("Enter a valid email address.", message);
    }

    [Fact]
    public void MultiplePasswordErrorsRemainSeparateMessages()
    {
        var messages = IdentityErrorFeedback.GetUserMessages(
        [
            new IdentityError { Code = "PasswordTooShort", Description = "Password is too short." },
            new IdentityError { Code = "PasswordRequiresDigit", Description = "Password needs a digit." }
        ]);

        Assert.Equal(2, messages.Count);
        Assert.Equal("Password is too short.", messages[0]);
        Assert.Equal("Password needs a digit.", messages[1]);
    }
}
