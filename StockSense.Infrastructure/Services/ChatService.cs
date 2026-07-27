using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Services;

public enum IntentType
{
    Greeting,
    PartsLookup,
    ServiceInfo,
    BuildGuidance,
    Appointment,
    Fallback
}

public sealed class ChatService
{
    private sealed class ChatSession
    {
        public object SyncRoot { get; } = new();
        public List<ChatMessage> Messages { get; } = new();
        public List<RagMatch> LastMatches { get; set; } = new();
        public DateTime LastAccessUtc { get; set; } = DateTime.UtcNow;
    }

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RagRetrievalService _retrieval;
    private readonly IAiChatProvider _aiProvider;
    private readonly ConcurrentDictionary<string, ChatSession> _sessions = new();

    public ChatService(IServiceScopeFactory scopeFactory, RagRetrievalService retrieval, IAiChatProvider aiProvider)
    {
        _scopeFactory = scopeFactory;
        _retrieval = retrieval;
        _aiProvider = aiProvider;
    }

    public async Task<ChatResponse> ProcessMessage(string message, string sessionId, string audience = "Customer")
    {
        CleanupExpiredSessions();
        var session = _sessions.GetOrAdd(sessionId, _ => new ChatSession());
        lock (session.SyncRoot)
        {
            session.LastAccessUtc = DateTime.UtcNow;
            session.Messages.Add(new ChatMessage { Role = "user", Content = message, Timestamp = DateTime.UtcNow });
        }

        var intent = ClassifyIntent(message);
        var reply = await GenerateReply(intent, message, session, audience);

        lock (session.SyncRoot)
        {
            session.Messages.Add(new ChatMessage
            {
                Role = "bot",
                Content = reply.Reply,
                Intent = reply.Intent,
                Sources = reply.Sources,
                Timestamp = DateTime.UtcNow,
            });
            if (session.Messages.Count > 50)
                session.Messages.RemoveRange(0, session.Messages.Count - 50);
        }
        return reply;
    }

    private static IntentType ClassifyIntent(string message)
    {
        var msg = message.Trim().ToLowerInvariant();
        if (Regex.IsMatch(msg, @"^(hi|hello|hey|good\s*(morning|afternoon|evening)|kamusta|musta|oy|helo)\b"))
            return IntentType.Greeting;
        if (Regex.IsMatch(msg, @"\b(book|appointment|schedule|visit|mechanic|slot|pa-service|paayos|patingin)\b"))
            return IntentType.Appointment;
        if (IsServiceDurationQuestion(msg))
            return IntentType.ServiceInfo;
        if (IsOilProductQuestion(msg))
            return IntentType.PartsLookup;
        if (Regex.IsMatch(msg, @"\b(oil|oils|change\s*oil|pms|service|services|maintenance|tune|tune-up|check-up|palit\s*langis)\b"))
            return IntentType.ServiceInfo;
        if (IsTargetCcBuildQuestion(msg))
            return IntentType.BuildGuidance;
        if (IsBuildGainQuestion(msg))
            return IntentType.BuildGuidance;
        if (Regex.IsMatch(msg, @"\b(part|parts|stock|inventory|reorder|restock|critical|price|cost|available|availability|have|meron|magkano|presyo|pyesa|product|products)\b"))
            return IntentType.PartsLookup;
        if (Regex.IsMatch(msg, @"\b(upgrade|build|package|custom|stage|compatible|compatibility|fit|work|gain|gains|setup|exhaust|muffler|pipe|ecu|cvt|clutch|variator|belt|cc|bore|crank|head|cam|piston|stroker|racing|speed|power|hp|torque|nmax|aerox|click|pcx|mio|raider|sniper)\b"))
            return IntentType.BuildGuidance;
        if (Regex.IsMatch(msg, @"\b(motorcycle|motorcycles|bike|bikes|model|models|acceptable|supported)\b"))
            return IntentType.BuildGuidance;
        return IntentType.Fallback;
    }

    private async Task<ChatResponse> GenerateReply(IntentType intent, string message, ChatSession session, string audience)
    {
        var isAdminAudience = string.Equals(audience, "Admin", StringComparison.OrdinalIgnoreCase);
        var useTaglish = !isAdminAudience && IsTaglishMessage(message);
        if (intent == IntentType.Greeting)
            return new ChatResponse
            {
                Intent = intent.ToString(),
                Reply = isAdminAudience
                    ? "Hi! I can help staff check inventory, low-stock items, service records, build parts, and appointment data."
                    : useTaglish
                        ? "Hi po! I can help you check available parts, prices, motorcycle compatibility, build suggestions, services, and appointments."
                        : "Hi! I can help you check available parts, prices, motorcycle compatibility, build suggestions, services, and appointments.",
                Suggestions = isAdminAudience
                    ? ["Low stock products", "Available JVT parts", "Service prices", "Appointment availability"]
                    : ["Available products", "Service prices", "Build for NMAX", "Book appointment"],
            };

        if (!isAdminAudience && (IsAdminOnlyInventoryQuestion(message) || IsAdminInventoryInsightQuestion(message)))
        {
            return new ChatResponse
            {
                Intent = IntentType.Fallback.ToString(),
                Reply = useTaglish
                    ? "Sorry po, pang staff/admin lang ang low-stock, restock planning, sales insights, and order recommendations. I can still help you check available products, prices, compatibility, services, and appointments."
                    : "Sorry, low-stock lists, restock planning, sales insights, and order recommendations are for staff/admin only. I can still help you check available products, prices, compatibility, services, and appointments.",
                Suggestions = ["Available products", "Service prices", "Build compatibility", "Book appointment"],
            };
        }

        if (isAdminAudience && (IsAdminOnlyInventoryQuestion(message) || IsAdminInventoryInsightQuestion(message)))
        {
            using var scope = _scopeFactory.CreateScope();
            var knowledgeBase = scope.ServiceProvider.GetRequiredService<KnowledgeBase>();
            return new ChatResponse
            {
                Intent = IntentType.PartsLookup.ToString(),
                Reply = await knowledgeBase.GetInventoryInsightsText(message),
                Suggestions = ["Best selling products", "What should we order next month?", "Low stock products", "Available tires"],
            };
        }

        if (IsOilProductQuestion(message))
        {
            using var scope = _scopeFactory.CreateScope();
            var knowledgeBase = scope.ServiceProvider.GetRequiredService<KnowledgeBase>();
            return new ChatResponse
            {
                Intent = IntentType.PartsLookup.ToString(),
                Reply = ApplyAudienceTone(await knowledgeBase.SearchPartsText(message), IntentType.PartsLookup, isAdminAudience, useTaglish),
                Suggestions = ["Show oil prices", "Book change oil service", "Oil change interval"],
            };
        }

        if (IsMaintenanceIntervalQuestion(message))
        {
            using var scope = _scopeFactory.CreateScope();
            var knowledgeBase = scope.ServiceProvider.GetRequiredService<KnowledgeBase>();
            return new ChatResponse
            {
                Intent = IntentType.ServiceInfo.ToString(),
                Reply = ApplyAudienceTone(await knowledgeBase.GetMaintenanceIntervalText(message), IntentType.ServiceInfo, isAdminAudience, useTaglish),
                Suggestions = ["Show active service prices", "Book appointment", "Available engine oil"],
            };
        }

        if (IsPipeOrExhaustQuestion(message))
        {
            using var scope = _scopeFactory.CreateScope();
            var knowledgeBase = scope.ServiceProvider.GetRequiredService<KnowledgeBase>();
            return new ChatResponse
            {
                Intent = IntentType.BuildGuidance.ToString(),
                Reply = ApplyAudienceTone(await knowledgeBase.GetBuildGuidanceText(message), IntentType.BuildGuidance, isAdminAudience, useTaglish),
                Suggestions = ["Show compatible exhaust", "Ask about ECU tuning", "Open build planner"],
            };
        }

        if (intent == IntentType.BuildGuidance && IsBuildGainQuestion(message))
        {
            using var scope = _scopeFactory.CreateScope();
            var knowledgeBase = scope.ServiceProvider.GetRequiredService<KnowledgeBase>();
            return new ChatResponse
            {
                Intent = intent.ToString(),
                Reply = ApplyAudienceTone(await knowledgeBase.GetBuildGuidanceText(message), intent, isAdminAudience, useTaglish),
                Suggestions = ["Open build planner", "Show compatible parts", "Talk to a mechanic"],
            };
        }

        if (intent == IntentType.BuildGuidance && IsGeneralBuildExplanationQuestion(message))
        {
            using var scope = _scopeFactory.CreateScope();
            var knowledgeBase = scope.ServiceProvider.GetRequiredService<KnowledgeBase>();
            var localAnswer = ApplyAudienceTone(await knowledgeBase.GetBuildGuidanceText(message), intent, isAdminAudience, useTaglish);
            if (HasVerifiedBuildAnswer(localAnswer))
            {
                return new ChatResponse
                {
                    Intent = intent.ToString(),
                    Reply = localAnswer,
                    Suggestions = ["Start building", "Show compatible parts", "Talk to a mechanic"],
                };
            }

            var response = await GenerateGeneralAnswerOrFallback(
                message,
                intent,
                localAnswer);
            return new ChatResponse
            {
                Intent = intent.ToString(),
                Reply = response.Reply,
                UsedLlm = response.UsedLlm,
                Suggestions = ["Available ECU products", "Build for NMAX", "Build for Aerox", "Book appointment"],
            };
        }

        if (intent == IntentType.BuildGuidance && IsTargetCcBuildQuestion(message))
        {
            using var scope = _scopeFactory.CreateScope();
            var knowledgeBase = scope.ServiceProvider.GetRequiredService<KnowledgeBase>();
            return new ChatResponse
            {
                Intent = intent.ToString(),
                Reply = ApplyAudienceTone(await knowledgeBase.GetBuildGuidanceText(message), intent, isAdminAudience, useTaglish),
                Suggestions = ["Start building", "Show compatible parts", "Talk to a mechanic"],
            };
        }

        List<RagMatch> priorMatches;
        lock (session.SyncRoot) priorMatches = session.LastMatches.ToList();
        IReadOnlyList<RagMatch> matches;
        if (priorMatches.Count > 0 && IsContextualFollowUp(message))
        {
            matches = priorMatches.Take(3).ToList();
        }
        else
        {
            using var scope = _scopeFactory.CreateScope();
            var knowledgeBase = scope.ServiceProvider.GetRequiredService<KnowledgeBase>();
            var documents = await knowledgeBase.GetRetrievalDocuments();
            matches = _retrieval.Search(message, documents);
            matches = FilterMatchesForIntent(intent, matches);
            if (intent == IntentType.BuildGuidance)
            {
                matches = matches
                    .OrderBy(match => match.Document.Type == "UpgradePart" ? 0 : match.Document.Type == "Build" ? 1 : 2)
                    .ThenByDescending(match => match.Score)
                    .ToList();
            }
        }

        if (matches.Count > 0)
        {
            lock (session.SyncRoot) session.LastMatches = matches.ToList();
            var grounded = BuildGroundedResponse(matches, isAdminAudience, useTaglish);
            if (RequiresVerifiedStockSenseRecords(message, intent) || intent == IntentType.BuildGuidance)
                return grounded;

            var llmAnswer = await _aiProvider.GenerateGroundedAnswerAsync(message, intent.ToString(), matches, grounded.Reply);
            if (!string.IsNullOrWhiteSpace(llmAnswer))
            {
                grounded.Reply = llmAnswer;
                grounded.Intent = $"LLM:{matches[0].Document.Type}";
                grounded.UsedLlm = true;
            }
            return grounded;
        }

        using (var scope = _scopeFactory.CreateScope())
        {
            var knowledgeBase = scope.ServiceProvider.GetRequiredService<KnowledgeBase>();
            if (intent == IntentType.PartsLookup)
            {
                var localAnswer = ApplyAudienceTone(await knowledgeBase.SearchPartsText(message), intent, isAdminAudience, useTaglish);
                var response = await GenerateGeneralAnswerOrFallback(message, intent, localAnswer);
                return new ChatResponse
                {
                    Intent = intent.ToString(),
                    Reply = response.Reply,
                    Suggestions = ["Check another product", "Show service prices", "Open build planner"],
                    UsedLlm = response.UsedLlm,
                };
            }

            if (intent == IntentType.ServiceInfo)
            {
                var localAnswer = ApplyAudienceTone(await knowledgeBase.SearchServiceInfo(message), intent, isAdminAudience, useTaglish);
                var response = await GenerateGeneralAnswerOrFallback(message, intent, localAnswer);
                return new ChatResponse
                {
                    Intent = intent.ToString(),
                    Reply = response.Reply,
                    Suggestions = ["Book appointment", "Oil change", "PMS service"],
                    UsedLlm = response.UsedLlm,
                };
            }

            if (intent == IntentType.BuildGuidance)
            {
                var localAnswer = ApplyAudienceTone(await knowledgeBase.GetBuildGuidanceText(message), intent, isAdminAudience, useTaglish);
                var response = await GenerateGeneralAnswerOrFallback(message, intent, localAnswer);
                return new ChatResponse
                {
                    Intent = intent.ToString(),
                    Reply = response.Reply,
                    Suggestions = ["Start building", "Show compatible parts", "Talk to a mechanic"],
                    UsedLlm = response.UsedLlm,
                };
            }

            if (intent == IntentType.Appointment)
            {
                return new ChatResponse
                {
                    Intent = intent.ToString(),
                    Reply = ApplyAudienceTone(await knowledgeBase.GetAppointmentAvailabilityText(), intent, isAdminAudience, useTaglish),
                    Suggestions = ["Open appointment page", "Show service prices", "Talk to a mechanic"],
                    Sources = [new ChatSource { Id = "page:appointment", Type = "Page", Title = "Book an appointment", Url = "/appointment", Relevance = 1 }],
                };
            }

            var fallback = await GenerateGeneralAnswerOrFallback(
                message,
                intent,
                "I can only answer StockSense questions about inventory, parts, motorcycle build compatibility, tuning gains, services, mechanics, and appointments. Please ask within those topics.");
            return new ChatResponse
            {
                Intent = IntentType.Fallback.ToString(),
                Reply = fallback.Reply,
                Suggestions = ["Find Yamaha parts", "Service prices", "Build guide", "Book appointment"],
                UsedLlm = fallback.UsedLlm,
            };
        }
    }

    private async Task<(string Reply, bool UsedLlm)> GenerateGeneralAnswerOrFallback(string message, IntentType intent, string localAnswer)
    {
        if (!IsMotorcycleRelated(message))
            return (localAnswer, false);

        if (RequiresVerifiedStockSenseRecords(message, intent))
            return (localAnswer, false);

        if (intent == IntentType.BuildGuidance && HasVerifiedBuildAnswer(localAnswer))
            return (localAnswer, false);

        var llmAnswer = await _aiProvider.GenerateGeneralMotorcycleAnswerAsync(message, intent.ToString());
        if (!string.IsNullOrWhiteSpace(llmAnswer))
            return (llmAnswer, true);

        return (BuildLocalMotorcycleFallback(message, intent, localAnswer), false);
    }

    private static bool RequiresVerifiedStockSenseRecords(string message, IntentType intent)
    {
        var msg = message.ToLowerInvariant();

        if (intent is IntentType.PartsLookup or IntentType.ServiceInfo or IntentType.Appointment)
            return true;

        return intent == IntentType.BuildGuidance &&
               Regex.IsMatch(msg, @"\b(available|availability|stock|price|cost|sell|have|meron|magkano|presyo|book|appointment|schedule)\b");
    }

    private static bool HasVerifiedBuildAnswer(string localAnswer)
        => localAnswer.Contains("StockSense build records", StringComparison.OrdinalIgnoreCase) ||
           localAnswer.Contains("StockSense build catalog", StringComparison.OrdinalIgnoreCase) ||
           localAnswer.Contains("StockSense upgrade parts", StringComparison.OrdinalIgnoreCase) ||
           localAnswer.Contains("prebuilt packages match", StringComparison.OrdinalIgnoreCase) ||
           localAnswer.Contains("compatible with", StringComparison.OrdinalIgnoreCase) ||
           localAnswer.Contains("compatible options", StringComparison.OrdinalIgnoreCase);

    private static string BuildLocalMotorcycleFallback(string message, IntentType intent, string localAnswer)
    {
        var msg = message.ToLowerInvariant();

        if (intent == IntentType.BuildGuidance &&
            Regex.IsMatch(msg, @"\b(what|how|why|benefit|gain|do|does|explain)\b") &&
            !Regex.IsMatch(msg, @"\b(available|stock|price|sell|have|meron|magkano|presyo)\b"))
        {
            if (msg.Contains("ecu"))
                return "ECU tuning adjusts how the engine manages fuel, ignition timing, throttle response, and rev limits. On a motorcycle, the gain depends on the engine setup: stock bikes usually get smoother response, while bikes with exhaust, intake, bore-up, or cam upgrades can gain more power when the tune is matched properly. For safety, tuning should be checked by a qualified mechanic and verified against the actual parts installed.";

            if (msg.Contains("exhaust") || msg.Contains("muffler") || msg.Contains("pipe"))
                return "An exhaust upgrade can change sound, reduce restriction, and sometimes improve power delivery, especially when matched with proper ECU/fuel tuning. The exact gain depends on the motorcycle model, exhaust design, and other installed parts.";

            if (msg.Contains("cvt") || msg.Contains("clutch") || msg.Contains("variator"))
                return "CVT tuning changes how a scooter transfers engine power to the wheel. Roller weights, springs, belt condition, clutch setup, and variator choice affect acceleration, cruising RPM, and top-end feel. The best setup depends on the rider goal and engine build.";
        }

        return localAnswer;
    }

    private static string ApplyAudienceTone(string reply, IntentType intent, bool isAdminAudience, bool useTaglish)
    {
        if (isAdminAudience || string.IsNullOrWhiteSpace(reply))
            return reply;

        if (!useTaglish)
        {
            if (reply.StartsWith("Matching StockSense inventory records:", StringComparison.OrdinalIgnoreCase))
                return reply.Replace("Matching StockSense inventory records:", "Yes, these are available in our inventory:");

            if (reply.StartsWith("I found these StockSense inventory records in that category:", StringComparison.OrdinalIgnoreCase))
                return reply.Replace("I found these StockSense inventory records in that category:", "Yes, these are the matching products in that category:");

            if (reply.StartsWith("Available StockSense pipe/exhaust inventory:", StringComparison.OrdinalIgnoreCase))
                return reply.Replace("Available StockSense pipe/exhaust inventory:", "Yes, these pipe/exhaust products are available:");

            if (reply.StartsWith("Available StockSense inventory products:", StringComparison.OrdinalIgnoreCase))
                return reply.Replace("Available StockSense inventory products:", "Yes, these products are available:");

            if (reply.StartsWith("Available StockSense engine-oil products", StringComparison.OrdinalIgnoreCase))
                return reply.Replace("Available StockSense engine-oil products", "Yes, these engine-oil products are available");

            if (reply.StartsWith("Current configured StockSense services:", StringComparison.OrdinalIgnoreCase))
                return reply.Replace("Current configured StockSense services:", "Yes, these services are available:");

            if (reply.StartsWith("Based on StockSense build records:", StringComparison.OrdinalIgnoreCase))
                return reply.Replace("Based on StockSense build records:", "Based on our build records, these are the compatible options:");

            if (reply.StartsWith("I could not find", StringComparison.OrdinalIgnoreCase) ||
                reply.StartsWith("I do not see", StringComparison.OrdinalIgnoreCase))
                return "Sorry, " + char.ToLowerInvariant(reply[0]) + reply[1..];

            return reply;
        }

        if (reply.StartsWith("Matching StockSense inventory records:", StringComparison.OrdinalIgnoreCase))
            return reply.Replace("Matching StockSense inventory records:", "Available sa StockSense inventory:");

        if (reply.StartsWith("I found these StockSense inventory records in that category:", StringComparison.OrdinalIgnoreCase))
            return reply.Replace("I found these StockSense inventory records in that category:", "Ito ang matching products sa category na yan:");

        if (reply.StartsWith("Available StockSense pipe/exhaust inventory:", StringComparison.OrdinalIgnoreCase))
            return reply.Replace("Available StockSense pipe/exhaust inventory:", "Available na pipe/exhaust sa inventory namin:");

        if (reply.StartsWith("Available StockSense inventory products:", StringComparison.OrdinalIgnoreCase))
            return reply.Replace("Available StockSense inventory products:", "Ito ang available products sa inventory namin:");

        if (reply.StartsWith("Available StockSense engine-oil products", StringComparison.OrdinalIgnoreCase))
            return reply.Replace("Available StockSense engine-oil products", "Ito ang available engine oil products namin");

        if (reply.StartsWith("Current configured StockSense services:", StringComparison.OrdinalIgnoreCase))
            return reply.Replace("Current configured StockSense services:", "Ito ang available services namin:");

        if (reply.StartsWith("Based on StockSense build records:", StringComparison.OrdinalIgnoreCase))
            return reply.Replace("Based on StockSense build records:", "Based sa build records namin, ito ang compatible options:");

        if (reply.StartsWith("For a mostly stock", StringComparison.OrdinalIgnoreCase))
            return "For daily use po, " + char.ToLowerInvariant(reply[0]) + reply[1..];

        if (reply.StartsWith("I could not find", StringComparison.OrdinalIgnoreCase) ||
            reply.StartsWith("I do not see", StringComparison.OrdinalIgnoreCase))
            return "Sorry po, " + char.ToLowerInvariant(reply[0]) + reply[1..];

        return reply;
    }

    private static ChatResponse BuildGroundedResponse(IReadOnlyList<RagMatch> matches, bool isAdminAudience, bool useTaglish)
    {
        var topType = matches[0].Document.Type;
        var selected = topType is "Product" or "Service"
            ? matches.Where(match => match.Document.Type == topType).Take(3).ToList()
            : matches.Take(3).ToList();
        var lines = selected.Select(match => match.Document.Type switch
        {
            "Product" => $"- {match.Document.Title}: PHP {match.Document.Price:N0}; {match.Document.CurrentStock ?? 0} currently in stock.",
            "Service" => $"- {match.Document.Title}: PHP {match.Document.Price:N0}; about {match.Document.DurationMinutes ?? 0} minutes.",
            "Build" => $"- {match.Document.Title}: estimated package total PHP {match.Document.Price:N0}.",
            "UpgradePart" => $"- {match.Document.Title}: build price PHP {match.Document.Price:N0}; stock {match.Document.CurrentStock ?? 0}; {ExtractGainText(match.Document.Text)}.",
            "Mechanic" => $"- {match.Document.Title}: active mechanic record.",
            "Appointment" => $"- {match.Document.Title}: {match.Document.DurationMinutes ?? 0} minutes, status details in appointment records.",
            _ => $"- {match.Document.Title}",
        });
        var intro = (topType, isAdminAudience, useTaglish) switch
        {
            ("Product", false, true) => "Ito ang nakita kong available products sa StockSense:",
            ("Service", false, true) => "Ito ang available services namin:",
            ("UpgradePart", false, true) => "Based sa build catalog namin, ito ang matching upgrade parts:",
            ("Build", false, true) => "Ito ang matching build packages namin:",
            ("Mechanic", false, true) => "Ito ang available mechanic records namin:",
            ("Appointment", false, true) => "Ito ang appointment records na nakita ko:",
            ("Product", false, false) => "Yes, these are the available products I found in StockSense:",
            ("Service", false, false) => "Yes, these are the available services:",
            ("UpgradePart", false, false) => "Based on our build catalog, these are the matching upgrade parts:",
            ("Build", false, false) => "These are the matching build packages:",
            ("Mechanic", false, false) => "These are the available mechanic records:",
            ("Appointment", false, false) => "These are the appointment records I found:",
            _ => "I found these verified StockSense records:",
        };

        return new ChatResponse
        {
            Intent = $"RAG:{topType}",
            UsedRetrieval = true,
            Reply = intro + "\n" + string.Join("\n", lines),
            Sources = selected.Select(match => new ChatSource
            {
                Id = match.Document.Id,
                Type = match.Document.Type,
                Title = match.Document.Title,
                Url = match.Document.Link,
                Relevance = Math.Round(match.Score, 2),
            }).ToList(),
            Suggestions = topType switch
            {
                "Product" => ["Is it in stock?", "How much is it?", "Open build planner"],
                "Service" => ["Book appointment", "How long does it take?", "What parts are required?"],
                "Build" => ["Open build planner", "Show included parts", "Talk to a mechanic"],
                "UpgradePart" => ["Will this fit my motorcycle?", "What gain will I get?", "Open build planner"],
                "Mechanic" => ["Book appointment", "Show booked slots", "Service prices"],
                "Appointment" => ["Open appointment page", "Show mechanics", "Service prices"],
                _ => ["Find parts", "Service prices"],
            },
        };
    }
    private static bool IsContextualFollowUp(string message)
        => Regex.IsMatch(message.ToLowerInvariant(), @"\b(it|that|those|them|same|this\s+one|how\s+much|how\s+long)\b");

    private static IReadOnlyList<RagMatch> FilterMatchesForIntent(IntentType intent, IReadOnlyList<RagMatch> matches)
    {
        return intent switch
        {
            IntentType.PartsLookup => matches
                .Where(match => match.Document.Type is "Product" or "UpgradePart" or "Build")
                .ToList(),
            IntentType.ServiceInfo => matches
                .Where(match => match.Document.Type == "Service")
                .ToList(),
            IntentType.Appointment => matches
                .Where(match => match.Document.Type is "Appointment" or "Mechanic")
                .ToList(),
            IntentType.BuildGuidance => matches
                .Where(match => match.Document.Type is "UpgradePart" or "Build" or "Product")
                .ToList(),
            _ => matches,
        };
    }

    private static string ExtractGainText(string text)
    {
        var match = Regex.Match(text, @"Gains\s+([^\.]+)\.", RegexOptions.IgnoreCase);
        return match.Success ? $"gains {match.Groups[1].Value.Trim()}" : "gain details are in the build record";
    }

    private static bool IsMotorcycleRelated(string message)
        => Regex.IsMatch(message.ToLowerInvariant(), @"\b(motor|motorcycle|bike|scooter|underbone|nmax|aerox|click|pcx|mio|raider|sniper|yamaha|honda|suzuki|kawasaki|vespa|kymco|engine|cvt|ecu|exhaust|muffler|pipe|oil|brake|tire|tyre|belt|clutch|variator|piston|cam|bore|crank|torque|horsepower|hp|cc|tuning|upgrade|maintenance|service|services|pms|appointment|mechanic|stock|inventory|product|products|item|items|part|parts|price|available)\b");

    private static bool IsAdminOnlyInventoryQuestion(string message)
        => Regex.IsMatch(message.ToLowerInvariant(), @"\b(low\s+stocks?|need\s+restock|needs?\s+restocking|products?\s+needs?\s+restocking|restock|reorder|critical\s+stock|what\s+to\s+stock|stocking\s+recommendation)\b");

    private static bool IsAdminInventoryInsightQuestion(string message)
        => Regex.IsMatch(message.ToLowerInvariant(), @"\b(best\s*selling|top\s*selling|fast\s*moving|best\s*orders?|what\s+should\s+i\s+order|order\s+next\s+month|recommended\s+order|purchase\s+next|stock\s+next)\b");

    private static bool IsTaglishMessage(string message)
        => Regex.IsMatch(message.ToLowerInvariant(), @"\b(po|opo|boss|kuya|ate|sir|ma'?am|magkano|presyo|meron|mayroon|pwede|puwede|ano|alin|saan|sa|ng|mga|langis|tambutso|pyesa|kargado|gusto|kailangan|salamat|paayos|patingin|palit|ilang|gaano)\b");

    private static bool IsMaintenanceIntervalQuestion(string message)
        => Regex.IsMatch(message.ToLowerInvariant(), @"\b(every\s+what\s+km|how\s+many\s+km|when\s+should|change\s+oil\s+interval|oil\s+change\s+interval|ilang\s+km)\b");

    private static bool IsServiceDurationQuestion(string message)
        => Regex.IsMatch(message.ToLowerInvariant(), @"\b(how\s+long|duration|time|minutes?|hours?|gaano\s+katagal|ilang\s+oras|ilang\s+minuto)\b") &&
           Regex.IsMatch(message.ToLowerInvariant(), @"\b(service|services|pms|change\s*oil|oil\s*change|cvt\s*clean|cvt\s*cleaning|cleaning|check[-\s]*up|tune[-\s]*up)\b");

    private static bool IsBuildGainQuestion(string message)
        => Regex.IsMatch(message.ToLowerInvariant(), @"\b(gain|gains|increase|improve|power|hp|torque|cc|setup)\b") &&
           Regex.IsMatch(message.ToLowerInvariant(), @"\b(cvt|ecu|clutch|variator|belt|pipe|exhaust|muffler|head|crank|block|bore|throttle\s*body|cam)\b");

    private static bool IsOilProductQuestion(string message)
    {
        var msg = message.ToLowerInvariant();
        return Regex.IsMatch(msg, @"\b(oil|oils|langis)\b") &&
               Regex.IsMatch(msg, @"\b(use|used|can\s+i\s+use|recommend|recommended|available|what|which|ano|pwede|puwede|for|sa)\b") &&
               !Regex.IsMatch(msg, @"\b(price|prices|presyo|magkano|labor|service\s+fee|fee|book|appointment|schedule)\b");
    }

    private static bool IsPipeOrExhaustQuestion(string message)
        => Regex.IsMatch(message.ToLowerInvariant(), @"\b(pipe|pipes|muffler|exhaust|tambutso)\b");

    private static bool IsGeneralBuildExplanationQuestion(string message)
    {
        var msg = message.ToLowerInvariant();
        return Regex.IsMatch(msg, @"\b(what\s+does|what\s+is|explain|how\s+does|why\s+use|benefits?\s+of)\b") &&
               !Regex.IsMatch(msg, @"\b(available|availability|stock|price|cost|sell|have|meron|magkano|presyo|gain|gains|compatible|fit|work\s+on|for\s+nmax|for\s+aerox|on\s+nmax|on\s+aerox)\b");
    }

    private static bool IsTargetCcBuildQuestion(string message)
        => Regex.IsMatch(message.ToLowerInvariant(), @"\b(become|reach|make|convert|upgrade\s+to|go\s+to|target)\b.*\b\d{3}\s*cc\b|\b\d{3}\s*cc\b.*\b(become|reach|make|convert|upgrade\s+to|go\s+to|target)\b");

    public List<ChatMessage> GetHistory(string sessionId)
    {
        CleanupExpiredSessions();
        var session = _sessions.GetOrAdd(sessionId, _ => new ChatSession());
        lock (session.SyncRoot)
        {
            session.LastAccessUtc = DateTime.UtcNow;
            return session.Messages.ToList();
        }
    }

    public bool ClearHistory(string sessionId) => _sessions.TryRemove(sessionId, out _);

    private void CleanupExpiredSessions()
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        foreach (var pair in _sessions.Where(pair => pair.Value.LastAccessUtc < cutoff))
            _sessions.TryRemove(pair.Key, out _);
    }
}
