using System.Text.RegularExpressions;

namespace StockSense.Infrastructure.Services;

public sealed class RagDocument
{
    public string Id { get; init; } = "";
    public string Type { get; init; } = "";
    public string Title { get; init; } = "";
    public string Text { get; init; } = "";
    public string Link { get; init; } = "";
    public decimal? Price { get; init; }
    public int? CurrentStock { get; init; }
    public int? DurationMinutes { get; init; }
}

public sealed class RagMatch
{
    public required RagDocument Document { get; init; }
    public double Score { get; init; }
    public IReadOnlyList<string> MatchedTerms { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Lightweight local retrieval for the shop catalog. It uses BM25-style term
/// weighting and bilingual query expansion, keeping answers grounded in live
/// StockSense records without requiring a cloud LLM or API key.
/// </summary>
public sealed partial class RagRetrievalService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "can", "do", "for", "i", "in", "is", "it", "me", "my",
        "of", "on", "please", "the", "to", "what", "with", "you", "your", "ba", "ko",
        "mo", "po", "yung", "ang", "ng", "sa", "may", "meron"
    };

    private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["langis"] = ["oil"],
        ["presyo"] = ["price", "cost"],
        ["magkano"] = ["price", "cost"],
        ["pyesa"] = ["part", "parts"],
        ["gulong"] = ["tire"],
        ["bola"] = ["fly", "ball"],
        ["tambutso"] = ["exhaust", "muffler"],
        ["linis"] = ["cleaner", "cleaning"],
        ["ayos"] = ["service", "repair"],
        ["paayos"] = ["service", "repair"],
        ["mura"] = ["price", "cost"],
        ["pms"] = ["maintenance", "service", "tune"],
        ["muffler"] = ["exhaust"],
        ["scooter"] = ["automatic", "cvt"],
    };

    public IReadOnlyList<RagMatch> Search(string query, IReadOnlyList<RagDocument> documents, int limit = 5)
    {
        if (documents.Count == 0) return Array.Empty<RagMatch>();
        var queryTerms = Expand(Tokenize(query)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (queryTerms.Length == 0) return Array.Empty<RagMatch>();

        var tokenized = documents.Select(document => Tokenize($"{document.Title} {document.Text}")).ToArray();
        var averageLength = tokenized.Average(tokens => Math.Max(tokens.Count, 1));
        var documentFrequency = queryTerms.ToDictionary(
            term => term,
            term => tokenized.Count(tokens => tokens.Contains(term, StringComparer.OrdinalIgnoreCase)),
            StringComparer.OrdinalIgnoreCase);

        const double k1 = 1.5;
        const double b = 0.75;
        var normalizedQuery = string.Join(' ', Tokenize(query));
        var matches = new List<RagMatch>();
        for (var index = 0; index < documents.Count; index++)
        {
            var tokens = tokenized[index];
            var frequencies = tokens.GroupBy(token => token, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
            var score = 0.0;
            var matchedTerms = new List<string>();
            foreach (var term in queryTerms)
            {
                if (!frequencies.TryGetValue(term, out var frequency)) continue;
                matchedTerms.Add(term);
                var frequencyInDocuments = documentFrequency[term];
                var idf = Math.Log(1 + (documents.Count - frequencyInDocuments + 0.5) / (frequencyInDocuments + 0.5));
                var denominator = frequency + k1 * (1 - b + b * tokens.Count / averageLength);
                score += idf * frequency * (k1 + 1) / denominator;
            }

            var normalizedTitle = string.Join(' ', Tokenize(documents[index].Title));
            if (normalizedQuery.Length >= 3 && normalizedTitle.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                score += 4.0;
            score += matchedTerms.Count(term => normalizedTitle.Split(' ').Contains(term, StringComparer.OrdinalIgnoreCase)) * 0.6;

            if (score >= 0.55)
                matches.Add(new RagMatch { Document = documents[index], Score = score, MatchedTerms = matchedTerms });
        }

        return matches.OrderByDescending(match => match.Score)
            .ThenBy(match => match.Document.Title)
            .Take(Math.Clamp(limit, 1, 10))
            .ToList();
    }

    private static IReadOnlyList<string> Tokenize(string value)
        => TokenRegex().Matches(value.ToLowerInvariant())
            .Select(match => match.Value)
            .Where(token => token.Length > 1 && !StopWords.Contains(token))
            .ToList();

    private static IEnumerable<string> Expand(IEnumerable<string> tokens)
    {
        foreach (var token in tokens)
        {
            yield return token;
            if (!Synonyms.TryGetValue(token, out var synonyms)) continue;
            foreach (var synonym in synonyms) yield return synonym;
        }
    }

    [GeneratedRegex(@"[a-z0-9]+(?:-[a-z0-9]+)?", RegexOptions.IgnoreCase)]
    private static partial Regex TokenRegex();
}
