using Microsoft.AspNetCore.Mvc;
using StockSense.Infrastructure.Services;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;

namespace StockSense.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;

    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> ProcessMessage([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });
        if (request.Message.Length > 1000)
            return BadRequest(new { error = "Message must be 1,000 characters or fewer." });

        var requestedAudience = string.Equals(request.Audience, "Admin", StringComparison.OrdinalIgnoreCase)
            ? "Admin"
            : "Customer";
        var canUseAdminAssistant = User.IsInRole("Admin") || User.IsInRole("Employee");
        var audience = requestedAudience == "Admin" && canUseAdminAssistant ? "Admin" : "Customer";

        var sessionId = string.IsNullOrWhiteSpace(request.SessionId) || request.SessionId.Length > 128
            ? Guid.NewGuid().ToString()
            : request.SessionId;
        var sessionKey = BuildSessionKey(audience, sessionId);

        var response = await _chatService.ProcessMessage(request.Message, sessionKey, audience);
        response.SessionId = sessionId;

        return Ok(response);
    }

    [HttpGet("history/{sessionId}")]
    public ActionResult<List<ChatMessage>> GetHistory(string sessionId, [FromQuery] string audience = "Customer")
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > 128)
            return BadRequest(new { error = "Invalid session ID." });

        return Ok(_chatService.GetHistory(BuildSessionKey(ResolveAudience(audience), sessionId)));
    }

    [HttpDelete("history/{sessionId}")]
    public IActionResult ClearHistory(string sessionId, [FromQuery] string audience = "Customer")
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > 128)
            return BadRequest(new { error = "Invalid session ID." });

        _chatService.ClearHistory(BuildSessionKey(ResolveAudience(audience), sessionId));
        return NoContent();
    }

    private string ResolveAudience(string requestedAudience)
    {
        var wantsAdmin = string.Equals(requestedAudience, "Admin", StringComparison.OrdinalIgnoreCase);
        var canUseAdminAssistant = User.IsInRole("Admin") || User.IsInRole("Employee");
        return wantsAdmin && canUseAdminAssistant ? "Admin" : "Customer";
    }

    private static string BuildSessionKey(string audience, string sessionId)
    {
        if (sessionId.StartsWith("customer_", StringComparison.OrdinalIgnoreCase) ||
            sessionId.StartsWith("admin_", StringComparison.OrdinalIgnoreCase))
            return sessionId;

        return $"{audience.ToLowerInvariant()}_{sessionId}";
    }
}
