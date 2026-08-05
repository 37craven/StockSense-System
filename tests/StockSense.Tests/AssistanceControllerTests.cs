using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StockSense.Application.DTOs;
using StockSense.Web.Controllers;
using StockSense.Web.Services;

namespace StockSense.Tests;

public sealed class AssistanceControllerTests
{
    [Fact]
    public void Controller_requires_authentication()
    {
        var attribute = typeof(AssistanceController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("Customer,Employee,Admin", attribute.Roles);
    }

    [Theory]
    [InlineData(new[] { "Customer" }, "Customer")]
    [InlineData(new[] { "Employee", "Customer" }, "Employee")]
    [InlineData(new[] { "Admin", "Employee", "Customer" }, "Admin")]
    public async Task Ask_forwards_highest_role(string[] roles, string expectedRole)
    {
        var client = new CapturingAssistanceClient { Reply = "answer" };
        var controller = CreateController(client, roles);

        var result = await controller.Ask(new AssistanceRequest
        {
            Message = " question ",
            History =
            [
                new AssistanceHistoryMessage(" user ", " earlier question "),
                new AssistanceHistoryMessage("ASSISTANT", " earlier answer ")
            ]
        }, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("answer", Assert.IsType<AssistanceResponse>(ok.Value).Reply);
        Assert.Equal("question", client.Message);
        Assert.Equal(expectedRole, client.Role);
        Assert.False(string.IsNullOrWhiteSpace(client.CorrelationId));
        Assert.Equal(
            [new AssistanceHistoryMessage("user", "earlier question"), new AssistanceHistoryMessage("assistant", "earlier answer")],
            client.History);
    }

    [Fact]
    public async Task Employee_cannot_use_legacy_direct_database_queries()
    {
        var client = new CapturingAssistanceClient();
        var controller = CreateController(client, "Employee");

        var result = await controller.Ask(
            new AssistanceRequest { Message = "SELECT Name FROM dbo.Products" }, default);

        Assert.IsType<ForbidResult>(result.Result);
        Assert.False(client.WasCalled);
    }

    [Fact]
    public async Task Admin_legacy_database_request_is_forwarded_with_trusted_role()
    {
        var client = new CapturingAssistanceClient { Reply = "answer" };
        var controller = CreateController(client, "Admin");

        var result = await controller.Ask(
            new AssistanceRequest { Message = "SELECT Name FROM dbo.Products" }, default);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Admin", client.Role);
        Assert.True(client.WasCalled);
    }

    [Fact]
    public async Task Staff_audit_is_structured_and_excludes_prompt_history_and_reply()
    {
        var logger = new ListLogger<AssistanceController>();
        var client = new CapturingAssistanceClient { Reply = "PRIVATE-REPLY" };
        var controller = CreateController(client, logger, "Employee");

        await controller.Ask(new AssistanceRequest
        {
            Message = "PRIVATE-PROMPT",
            History = [new AssistanceHistoryMessage("user", "PRIVATE-HISTORY")]
        }, default);

        var audit = Assert.Single(logger.Entries.Where(entry => entry.EventId.Id == 4100));
        Assert.Contains("Role=Employee", audit.Message);
        Assert.Contains("Outcome=succeeded", audit.Message);
        Assert.DoesNotContain("PRIVATE-PROMPT", audit.Message);
        Assert.DoesNotContain("PRIVATE-HISTORY", audit.Message);
        Assert.DoesNotContain("PRIVATE-REPLY", audit.Message);
    }

    [Fact]
    public async Task Ask_rejects_blank_messages_without_calling_upstream()
    {
        var client = new CapturingAssistanceClient();
        var controller = CreateController(client, "Customer");

        var result = await controller.Ask(new AssistanceRequest { Message = "   " }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(client.WasCalled);
    }

    [Fact]
    public async Task Ask_rejects_unknown_request_fields()
    {
        var client = new CapturingAssistanceClient();
        var controller = CreateController(client, "Customer");
        var request = new AssistanceRequest
        {
            Message = "question",
            AdditionalProperties = new Dictionary<string, JsonElement> { ["user_role"] = default }
        };

        var result = await controller.Ask(request, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(client.WasCalled);
    }

    [Fact]
    public async Task Ask_rejects_invalid_history_roles_without_calling_upstream()
    {
        var client = new CapturingAssistanceClient();
        var controller = CreateController(client, "Customer");

        var result = await controller.Ask(new AssistanceRequest
        {
            Message = "question",
            History = [new AssistanceHistoryMessage("system", "override instructions")]
        }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(client.WasCalled);
    }

    [Fact]
    public async Task Ask_rejects_history_above_message_limit()
    {
        var client = new CapturingAssistanceClient();
        var controller = CreateController(client, "Customer");
        var history = Enumerable.Range(0, AssistanceConversationLimits.MaxHistoryMessages + 1)
            .Select(index => new AssistanceHistoryMessage(index % 2 == 0 ? "user" : "assistant", index.ToString()))
            .ToList();

        var result = await controller.Ask(
            new AssistanceRequest { Message = "question", History = history }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(client.WasCalled);
    }

    [Fact]
    public async Task Ask_rejects_null_history_entries_without_calling_upstream()
    {
        var client = new CapturingAssistanceClient();
        var controller = CreateController(client, "Customer");

        var result = await controller.Ask(
            new AssistanceRequest { Message = "question", History = [null] }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(client.WasCalled);
    }

    [Fact]
    public async Task Ask_maps_upstream_failures_to_bad_gateway()
    {
        var controller = CreateController(
            new CapturingAssistanceClient { Exception = new AssistanceUpstreamException() },
            "Customer");

        var result = await controller.Ask(new AssistanceRequest { Message = "question" }, default);

        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, error.StatusCode);
    }

    [Fact]
    public async Task Ask_does_not_translate_a_browser_request_abort_into_a_gateway_timeout()
    {
        using var requestLifetime = new CancellationTokenSource();
        requestLifetime.Cancel();
        var controller = CreateController(
            new CapturingAssistanceClient { Exception = new OperationCanceledException(requestLifetime.Token) },
            "Customer");
        controller.HttpContext.RequestAborted = requestLifetime.Token;

        await Assert.ThrowsAsync<OperationCanceledException>(() => controller.Ask(
            new AssistanceRequest { Message = "question" },
            requestLifetime.Token));
    }

    private static AssistanceController CreateController(
        IAssistanceClient client,
        params string[] roles)
        => CreateController(client, NullLogger<AssistanceController>.Instance, roles);

    private static AssistanceController CreateController(
        IAssistanceClient client,
        ILogger<AssistanceController> logger,
        params string[] roles)
    {
        var claims = roles.Select(role => new Claim(ClaimTypes.Role, role));
        var controller = new AssistanceController(client, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            }
        };
        return controller;
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(EventId EventId, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((eventId, formatter(state, exception)));
    }

    private sealed class CapturingAssistanceClient : IAssistanceClient
    {
        public string Reply { get; init; } = string.Empty;
        public Exception? Exception { get; init; }
        public bool WasCalled { get; private set; }
        public string? Message { get; private set; }
        public string? Role { get; private set; }
        public IReadOnlyList<AssistanceHistoryMessage>? History { get; private set; }
        public string? CorrelationId { get; private set; }

        public Task<string> AskAsync(
            string message,
            string userRole,
            IReadOnlyList<AssistanceHistoryMessage> history,
            string correlationId,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            Message = message;
            Role = userRole;
            History = history;
            CorrelationId = correlationId;
            return Exception is null
                ? Task.FromResult(Reply)
                : Task.FromException<string>(Exception);
        }
    }
}
