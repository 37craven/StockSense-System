using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using StockSense.Web.Services;

namespace StockSense.Tests;

public sealed class AssistanceClientTests
{
    [Fact]
    public async Task AskAsync_preserves_base_url_path_without_trailing_slash()
    {
        var handler = new RecordingHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://internal.example/chatbot")
        };
        var client = new AssistanceClient(httpClient, NullLogger<AssistanceClient>.Instance);

        var reply = await client.AskAsync("question", "Employee", default);

        Assert.Equal("answer", reply);
        Assert.Equal(new Uri("https://internal.example/chatbot/api/chat"), handler.RequestUri);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"reply\":\"answer\"}", Encoding.UTF8, "application/json")
            });
        }
    }
}
