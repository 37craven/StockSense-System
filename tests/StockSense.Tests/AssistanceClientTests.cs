using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using StockSense.Application.DTOs;
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

        var history = new[]
        {
            new AssistanceHistoryMessage("user", "2022 V2"),
            new AssistanceHistoryMessage("assistant", "first answer")
        };
        var reply = await client.AskAsync("question", "Employee", history, default);

        Assert.Equal("answer", reply);
        Assert.Equal(new Uri("https://internal.example/chatbot/api/chat"), handler.RequestUri);
        using var payload = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("Employee", payload.RootElement.GetProperty("user_role").GetString());
        Assert.Equal("question", payload.RootElement.GetProperty("message").GetString());
        var sentHistory = payload.RootElement.GetProperty("history");
        Assert.Equal(2, sentHistory.GetArrayLength());
        Assert.Equal("user", sentHistory[0].GetProperty("role").GetString());
        Assert.Equal("2022 V2", sentHistory[0].GetProperty("content").GetString());
        Assert.Equal("first answer", sentHistory[1].GetProperty("content").GetString());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"reply\":\"answer\"}", Encoding.UTF8, "application/json")
            };
        }
    }
}
