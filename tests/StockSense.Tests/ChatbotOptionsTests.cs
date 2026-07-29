using StockSense.Web.Options;

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
}
