using System.Net;
using System.Text;
using System.Text.Json;
using StockSense.Client.Components;

namespace StockSense.Tests;

public sealed class ApiErrorMessageTests
{
    public static TheoryData<HttpStatusCode, string> StatusMessages => new()
    {
        { HttpStatusCode.BadRequest, "Check the information and try again." },
        { HttpStatusCode.Unauthorized, "Your session has expired. Please sign in again." },
        { HttpStatusCode.Forbidden, "You do not have permission to do that." },
        { HttpStatusCode.NotFound, "The requested item was not found." },
        { HttpStatusCode.Conflict, "This information changed. Reload and try again." },
        { HttpStatusCode.UnprocessableEntity, "Check the highlighted information and try again." },
        { HttpStatusCode.TooManyRequests, "Too many requests. Wait a moment and try again." },
        { HttpStatusCode.InternalServerError, "Something went wrong. Please try again." }
    };

    [Theory]
    [MemberData(nameof(StatusMessages))]
    public async Task Status_fallbacks_never_expose_codes(HttpStatusCode status, string expected)
    {
        using var response = new HttpResponseMessage(status);

        var message = await ApiErrorMessage.FromResponseAsync(response);

        Assert.Equal(expected, message);
        Assert.DoesNotContain(((int)status).ToString(), message);
    }

    [Fact]
    public async Task Conflict_preserves_a_safe_actionable_api_message()
    {
        using var response = JsonResponse(
            HttpStatusCode.Conflict,
            "{\"error\":\"The product was changed by another user. Reload and try again.\"}");

        var message = await ApiErrorMessage.FromResponseAsync(response);

        Assert.Equal("The product was changed by another user. Reload and try again.", message);
    }

    [Theory]
    [InlineData("{\"error\":\"Request failed (409).\"}")]
    [InlineData("{\"error\":\"System.InvalidOperationException: failed at Sap Shop.Service\"}")]
    [InlineData("{\"title\":\"Internal Server Error\",\"traceId\":\"abc\"}")]
    [InlineData("<html><body>500 Server Error</body></html>")]
    public async Task Technical_or_raw_payloads_use_a_safe_fallback(string body)
    {
        using var response = JsonResponse(HttpStatusCode.InternalServerError, body);

        var message = await ApiErrorMessage.FromResponseAsync(response);

        Assert.Equal("Something went wrong. Please try again.", message);
    }

    [Fact]
    public async Task Problem_details_validation_errors_are_combined_without_json()
    {
        using var response = JsonResponse(
            HttpStatusCode.BadRequest,
            "{\"errors\":{\"Name\":[\"Name is required.\"],\"Price\":[\"Price must be positive.\"]}}");

        var message = await ApiErrorMessage.FromResponseAsync(response);

        Assert.Equal("Name is required. Price must be positive.", message);
    }

    public static TheoryData<Exception, string> TransportMessages => new()
    {
        { new HttpRequestException("Connection refused at 127.0.0.1"), "Unable to reach the server. Check your connection and try again." },
        { new TaskCanceledException("fetch aborted"), "The request was cancelled." },
        { new JsonException("'<' is invalid JSON"), "The server returned an unexpected response. Please try again." },
        { new InvalidOperationException("System.Data.SqlClient failure"), "Something went wrong. Please try again." }
    };

    [Theory]
    [MemberData(nameof(TransportMessages))]
    public void Exception_details_are_not_shown(Exception exception, string expected)
    {
        Assert.Equal(expected, ApiErrorMessage.FromException(exception));
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string content) => new(status)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };
}
