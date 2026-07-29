using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
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

        var result = await controller.Ask(new AssistanceRequest { Message = " question " }, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("answer", Assert.IsType<AssistanceResponse>(ok.Value).Reply);
        Assert.Equal("question", client.Message);
        Assert.Equal(expectedRole, client.Role);
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
    public async Task Ask_maps_upstream_failures_to_bad_gateway()
    {
        var controller = CreateController(
            new CapturingAssistanceClient { Exception = new AssistanceUpstreamException() },
            "Customer");

        var result = await controller.Ask(new AssistanceRequest { Message = "question" }, default);

        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, error.StatusCode);
    }

    private static AssistanceController CreateController(
        IAssistanceClient client,
        params string[] roles)
    {
        var claims = roles.Select(role => new Claim(ClaimTypes.Role, role));
        var controller = new AssistanceController(client, NullLogger<AssistanceController>.Instance)
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

    private sealed class CapturingAssistanceClient : IAssistanceClient
    {
        public string Reply { get; init; } = string.Empty;
        public Exception? Exception { get; init; }
        public bool WasCalled { get; private set; }
        public string? Message { get; private set; }
        public string? Role { get; private set; }

        public Task<string> AskAsync(string message, string userRole, CancellationToken cancellationToken)
        {
            WasCalled = true;
            Message = message;
            Role = userRole;
            return Exception is null
                ? Task.FromResult(Reply)
                : Task.FromException<string>(Exception);
        }
    }
}
