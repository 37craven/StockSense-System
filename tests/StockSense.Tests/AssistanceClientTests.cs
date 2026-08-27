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
        var reply = await client.AskAsync("question", "Employee", history, null, "", "", "", "corr-123", default);

        Assert.Equal("answer", reply.Reply);
        Assert.Equal(new Uri("https://internal.example/chatbot/api/chat"), handler.RequestUri);
        Assert.Equal("corr-123", handler.CorrelationId);
        using var payload = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("Employee", payload.RootElement.GetProperty("user_role").GetString());
        Assert.Equal("question", payload.RootElement.GetProperty("message").GetString());
        var sentHistory = payload.RootElement.GetProperty("history");
        Assert.Equal(2, sentHistory.GetArrayLength());
        Assert.Equal("user", sentHistory[0].GetProperty("role").GetString());
        Assert.Equal("2022 V2", sentHistory[0].GetProperty("content").GetString());
        Assert.Equal("first answer", sentHistory[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task AskAsync_uses_chat_endpoint_for_trailing_slash_base_url()
    {
        var handler = new RecordingHandler();
        var client = CreateClient(handler, "https://internal.example/chatbot/");

        await client.AskAsync("question", "Customer", [], null, "", "", "", "corr", default);

        Assert.Equal(new Uri("https://internal.example/chatbot/api/chat"), handler.RequestUri);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task AskAsync_maps_non_success_status_to_safe_upstream_exception(HttpStatusCode status)
    {
        var client = CreateClient(new StaticHandler(new HttpResponseMessage(status)));

        await Assert.ThrowsAsync<AssistanceUpstreamException>(() =>
            client.AskAsync("SECRET PROMPT", "Customer", [], null, "", "", "", "corr", default));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("{\"reply\":\"   \"}")]
    [InlineData("not-json")]
    public async Task AskAsync_rejects_empty_or_malformed_responses(string body)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var client = CreateClient(new StaticHandler(response));

        await Assert.ThrowsAsync<AssistanceUpstreamException>(() =>
            client.AskAsync("question", "Customer", [], null, "", "", "", "corr", default));
    }

    [Fact]
    public async Task AskAsync_propagates_cancellation_token_to_http_handler()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var client = CreateClient(new CancellationHandler());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.AskAsync("question", "Customer", [], null, "", "", "", "corr", cancellation.Token));
    }

    [Fact]
    public async Task AskAsync_requires_a_configured_base_address()
    {
        var client = new AssistanceClient(new HttpClient(new RecordingHandler()), NullLogger<AssistanceClient>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.AskAsync("question", "Customer", [], null, "", "", "", "corr", default));
    }

    private static AssistanceClient CreateClient(HttpMessageHandler handler, string baseUrl = "https://internal.example/") =>
        new(new HttpClient(handler) { BaseAddress = new Uri(baseUrl) }, NullLogger<AssistanceClient>.Instance);

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? RequestBody { get; private set; }
        public string? CorrelationId { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            CorrelationId = request.Headers.GetValues("X-Correlation-ID").Single();
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"reply\":\"answer\",\"actions\":null,\"workflowState\":null}", Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class StaticHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }

    private sealed class CancellationHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromCanceled<HttpResponseMessage>(cancellationToken);
    }
}
