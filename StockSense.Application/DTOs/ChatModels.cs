using StockSense.Domain.Entities;
namespace StockSense.Application.DTOs
{
    public class ChatMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Intent { get; set; }
        public List<ChatSource> Sources { get; set; } = new();
    }

    public class ChatSource
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public double Relevance { get; set; }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
        public string? SessionId { get; set; }
        public string Audience { get; set; } = "Customer";
    }

    public class ChatResponse
    {
        public string Reply { get; set; } = "";
        public List<string> Suggestions { get; set; } = new();
        public string Intent { get; set; } = "Fallback";
        public string? SessionId { get; set; }
        public List<ChatSource> Sources { get; set; } = new();
        public bool UsedRetrieval { get; set; }
        public bool UsedLlm { get; set; }
    }
}
