using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Web.Services;

namespace StockSense.Web.Controllers;

[ApiController]
[Authorize(Roles = "Customer,Employee,Admin")]
[Route("api/assistance")]
public sealed class AssistanceController(
    IAssistanceClient assistanceClient,
    ILogger<AssistanceController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AssistanceResponse>> Ask(
        [FromBody] AssistanceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 })
            return BadRequest(new { error = "Only the message field is accepted." });

        var message = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message))
            return BadRequest(new { error = "Message is required." });

        try
        {
            var reply = await assistanceClient.AskAsync(
                message,
                GetHighestRole(User),
                cancellationToken);
            return Ok(new AssistanceResponse(reply));
        }
        catch (OperationCanceledException) when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning("Chatbot request timed out.");
            return StatusCode(StatusCodes.Status504GatewayTimeout, new { error = "Assistance service timed out." });
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Chatbot service could not be reached.");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Assistance service is unavailable." });
        }
        catch (AssistanceUpstreamException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Assistance service is unavailable." });
        }
    }

    internal static string GetHighestRole(ClaimsPrincipal user)
    {
        if (user.IsInRole("Admin")) return "Admin";
        if (user.IsInRole("Employee")) return "Employee";
        return "Customer";
    }
}

public sealed class AssistanceRequest
{
    [Required, StringLength(8000, MinimumLength = 1)]
    public string? Message { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record AssistanceResponse(string Reply);
