using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Application.DTOs;
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
            return BadRequest(new { error = "Only the message and history fields are accepted." });

        var message = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message))
            return BadRequest(new { error = "Message is required." });

        if (message.Length > AssistanceConversationLimits.MaxMessageLength)
            return BadRequest(new { error = "Message is too long." });

        var history = request.History ?? [];
        if (history.Count > AssistanceConversationLimits.MaxHistoryMessages)
            return BadRequest(new { error = $"History cannot contain more than {AssistanceConversationLimits.MaxHistoryMessages} messages." });

        var normalizedHistory = new List<AssistanceHistoryMessage>(history.Count);
        var historyCharacters = 0;
        foreach (var item in history)
        {
            if (item is null)
                return BadRequest(new { error = "History messages cannot be null." });
            var role = item.Role?.Trim().ToLowerInvariant();
            var content = item.Content?.Trim();
            if (role is not ("user" or "assistant"))
                return BadRequest(new { error = "History roles must be user or assistant." });
            if (string.IsNullOrWhiteSpace(content))
                return BadRequest(new { error = "History messages cannot be blank." });
            if (content.Length > AssistanceConversationLimits.MaxMessageLength)
                return BadRequest(new { error = "A history message is too long." });

            historyCharacters += content.Length;
            if (historyCharacters > AssistanceConversationLimits.MaxHistoryCharacters)
                return BadRequest(new { error = "Conversation history is too large." });
            normalizedHistory.Add(new AssistanceHistoryMessage(role, content));
        }

        try
        {
            var reply = await assistanceClient.AskAsync(
                message,
                GetHighestRole(User),
                normalizedHistory,
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

    public IReadOnlyList<AssistanceHistoryMessage?>? History { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record AssistanceResponse(string Reply);
