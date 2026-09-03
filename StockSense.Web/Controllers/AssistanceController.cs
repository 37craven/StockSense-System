using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Application.DTOs;
using StockSense.Web.Services;

namespace StockSense.Web.Controllers;

[ApiController]
[Authorize(Roles = "Customer,Employee,Admin")]
[AllowAnonymous]
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
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var role = isAuthenticated ? GetHighestRole(User) : "Guest";
        var correlationId = string.IsNullOrWhiteSpace(HttpContext.TraceIdentifier)
            ? Guid.NewGuid().ToString("N")
            : HttpContext.TraceIdentifier;
        HttpContext.Response.Headers["X-Correlation-ID"] = correlationId;
        var actorHash = GetAuditActorHash(User);

        if (request.AdditionalProperties is { Count: > 0 })
            return RejectBadRequest(role, actorHash, correlationId, "unexpected-fields",
                "Only the message and history fields are accepted.");

        var message = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message))
            return RejectBadRequest(role, actorHash, correlationId, "blank-message", "Message is required.");

        if (message.Length > AssistanceConversationLimits.MaxMessageLength)
            return RejectBadRequest(role, actorHash, correlationId, "message-limit", "Message is too long.");

        // Employees use the fixed operational reports exposed by the chatbot.
        // Ad-hoc SQL is an administrator-only legacy capability and must not be
        // authorized by prompt text or client-supplied conversation history.
        if (role == "Employee" && ContainsDirectDatabaseQuery(message))
        {
            AuditStaffAccess(role, actorHash, correlationId, "denied", "employee-direct-database-query");
            return Forbid();
        }

        var history = request.History ?? [];
        if (history.Count > AssistanceConversationLimits.MaxHistoryMessages)
            return RejectBadRequest(role, actorHash, correlationId, "history-count-limit",
                $"History cannot contain more than {AssistanceConversationLimits.MaxHistoryMessages} messages.");

        var normalizedHistory = new List<AssistanceHistoryMessage>(history.Count);
        var historyCharacters = 0;
        foreach (var item in history)
        {
            if (item is null)
                return RejectBadRequest(role, actorHash, correlationId, "null-history", "History messages cannot be null.");
            var historyRole = item.Role?.Trim().ToLowerInvariant();
            var content = item.Content?.Trim();
            if (historyRole is not ("user" or "assistant"))
                return RejectBadRequest(role, actorHash, correlationId, "invalid-history-role",
                    "History roles must be user or assistant.");
            if (string.IsNullOrWhiteSpace(content))
                return RejectBadRequest(role, actorHash, correlationId, "blank-history",
                    "History messages cannot be blank.");
            if (content.Length > AssistanceConversationLimits.MaxMessageLength)
                return RejectBadRequest(role, actorHash, correlationId, "history-message-limit",
                    "A history message is too long.");

            historyCharacters += content.Length;
            if (historyCharacters > AssistanceConversationLimits.MaxHistoryCharacters)
                return RejectBadRequest(role, actorHash, correlationId, "history-size-limit",
                    "Conversation history is too large.");
            normalizedHistory.Add(new AssistanceHistoryMessage(historyRole, content));
        }

        try
        {
            var customerName = isAuthenticated ? User.FindFirstValue(ClaimTypes.Name) ?? "" : "";
            var customerEmail = isAuthenticated ? User.FindFirstValue(ClaimTypes.Email) ?? "" : "";
            var customerId = isAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "" : "";

            var (reply, actions, workflowState) = await assistanceClient.AskAsync(
                message,
                role,
                normalizedHistory,
                request.WorkflowState,
                customerName,
                customerEmail,
                customerId,
                correlationId,
                cancellationToken);

            var actionSummary = actions?.Select(a => $"{a.ActionType}:{a.Label}") ?? [];
            logger.LogWarning("[ASSIST-CTRL] Returning to frontend: Reply={Reply} Actions=[{Actions}] WfStatus={WfStatus}",
                reply?.Length > 80 ? reply[..80] : reply,
                string.Join(", ", actionSummary),
                workflowState?.Status);

            AuditStaffAccess(role, actorHash, correlationId, "succeeded", "completed");
            return Ok(new AssistanceResponse(reply, actions, workflowState));
        }
        catch (OperationCanceledException) when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            AuditStaffAccess(role, actorHash, correlationId, "failed", "timeout");
            logger.LogWarning("Chatbot request timed out.");
            return StatusCode(StatusCodes.Status504GatewayTimeout, new { error = "Assistance service timed out." });
        }
        catch (HttpRequestException exception)
        {
            AuditStaffAccess(role, actorHash, correlationId, "failed", "unreachable");
            logger.LogWarning(exception, "Chatbot service could not be reached.");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Assistance service is unavailable." });
        }
        catch (AssistanceUpstreamException)
        {
            AuditStaffAccess(role, actorHash, correlationId, "failed", "upstream-error");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Assistance service is unavailable." });
        }
    }

    internal static bool ContainsDirectDatabaseQuery(string message)
    {
        try
        {
            return ExplicitSqlRequestRegex.IsMatch(message)
                || SchemaQualifiedFromRegex.IsMatch(message)
                || StandaloneSelectExpressionRegex.IsMatch(message)
                || SelectProjectionSyntaxRegex.IsMatch(message)
                || SelectsFromKnownTable(message);
        }
        catch (RegexMatchTimeoutException)
        {
            // This is a security boundary. If the one bounded backtracking
            // matcher reaches its deadline, deny instead of returning a 500.
            return true;
        }
    }

    // Match database language by structure instead of treating ordinary shop
    // words such as "select" and "from" as SQL on their own. The bounded
    // SELECT-to-FROM scan keeps the check predictable for the 8,000-character
    // request limit while accepting normal casing and whitespace variations.
    private const RegexOptions DirectQueryRegexOptions =
        RegexOptions.IgnoreCase
        | RegexOptions.CultureInvariant
        | RegexOptions.NonBacktracking;

    private static readonly Regex ExplicitSqlRequestRegex = new(
        @"\bsql\b",
        DirectQueryRegexOptions,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex SchemaQualifiedFromRegex = new(
        @"\bfrom\s+\[?dbo\]?\s*\.\s*\[?[a-z_][a-z0-9_]*\]?",
        DirectQueryRegexOptions,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex StandaloneSelectExpressionRegex = new(
        @"\bselect\b\s+(?:@@[a-z_][a-z0-9_]*|\d+(?:\.\d+)?|'(?:''|[^'])*'|(?:count|sum|avg|min|max|getdate|newid)\s*\()",
        DirectQueryRegexOptions,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex SelectProjectionSyntaxRegex = new(
        @"\bselect\b\s+(?:(?:top\s*\(?\d+\)?|distinct)\s+)*(?:\*|\[[^\]\r\n]{1,128}\]|[a-z_][a-z0-9_]*\s*\.\s*[a-z_][a-z0-9_]*|[a-z_][a-z0-9_]*(?:\s*,\s*[a-z_][a-z0-9_]*)+)\s+from\b",
        DirectQueryRegexOptions,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex SelectFromTargetRegex = new(
        @"\bselect\b[\s\S]{0,500}?\bfrom\s+(?:(?:\[?dbo\]?\s*\.\s*)?\[?(?<table>[a-z_][a-z0-9_]*)\]?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly HashSet<string> KnownDatabaseTables = new(
        [
            "products", "suppliers", "transactions", "transactionitems",
            "orderslips", "orderslipitems", "appointments", "buildrequests",
            "prebuiltpackages", "prebuiltpackageproduct", "motorcycles"
        ],
        StringComparer.OrdinalIgnoreCase);

    private static bool SelectsFromKnownTable(string message)
    {
        foreach (Match match in SelectFromTargetRegex.Matches(message))
        {
            if (KnownDatabaseTables.Contains(match.Groups["table"].Value))
                return true;
        }

        return false;
    }

    internal static string GetAuditActorHash(ClaimsPrincipal user)
    {
        var actor = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.Identity?.Name
            ?? "unknown";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(actor)))[..16];
    }

    private ActionResult<AssistanceResponse> RejectBadRequest(
        string role,
        string actorHash,
        string correlationId,
        string reason,
        string publicMessage)
    {
        AuditStaffAccess(role, actorHash, correlationId, "rejected", reason);
        return BadRequest(new { error = publicMessage });
    }

    private void AuditStaffAccess(
        string role,
        string actorHash,
        string correlationId,
        string outcome,
        string reason)
    {
        if (role is not ("Admin" or "Employee"))
            return;

        logger.LogInformation(
            new EventId(4100, "StaffAssistanceAccess"),
            "Staff assistance access. Role={Role} ActorHash={ActorHash} CorrelationId={CorrelationId} Outcome={Outcome} Reason={Reason}",
            role, actorHash, correlationId, outcome, reason);
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

    public WorkflowState? WorkflowState { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record AssistanceResponse(
    string Reply,
    IReadOnlyList<ChatAction>? Actions = null,
    WorkflowState? WorkflowState = null);
