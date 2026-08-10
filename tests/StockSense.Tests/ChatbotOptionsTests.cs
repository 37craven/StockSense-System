using StockSense.Web.Options;
using System.ComponentModel.DataAnnotations;

namespace StockSense.Tests;

public sealed class ChatbotOptionsTests
{
    [Theory]
    [InlineData("http://127.0.0.1:8000", true)]
    [InlineData("https://internal.example/chatbot", true)]
    [InlineData("ftp://internal.example/chatbot", false)]
    [InlineData("file:///chatbot", false)]
    public void HasSupportedScheme_allows_only_http_and_https(string baseUrl, bool expected)
    {
        var options = new ChatbotOptions { BaseUrl = new Uri(baseUrl) };

        Assert.Equal(expected, options.HasSupportedScheme());
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(30, true)]
    [InlineData(120, true)]
    [InlineData(0, false)]
    [InlineData(121, false)]
    public void TimeoutSeconds_obeys_declared_range(int timeoutSeconds, bool expectedValid)
    {
        var options = new ChatbotOptions { TimeoutSeconds = timeoutSeconds };

        Assert.Equal(expectedValid, Validator.TryValidateObject(options, new(options), null, true));
    }

    [Theory]
    [InlineData("https://internal.example/chatbot")]
    [InlineData("https://internal.example/chatbot/")]
    public void BaseUrl_accepts_absolute_urls_with_or_without_trailing_slash(string baseUrl)
    {
        var options = new ChatbotOptions { BaseUrl = new Uri(baseUrl) };

        Assert.True(options.BaseUrl.IsAbsoluteUri);
        Assert.True(options.HasSupportedScheme());
    }
}
