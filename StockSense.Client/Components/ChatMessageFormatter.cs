using System.Text.RegularExpressions;

namespace StockSense.Client.Components;

public static partial class ChatMessageFormatter
{
    public static IReadOnlyList<ChatContentBlock> Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return [];

        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
        var blocks = new List<ChatContentBlock>();

        for (var index = 0; index < lines.Length;)
        {
            if (string.IsNullOrWhiteSpace(lines[index]))
            {
                index++;
                continue;
            }

            var trimmed = lines[index].Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                var language = trimmed[3..].Trim();
                var code = new List<string>();
                index++;
                while (index < lines.Length && !lines[index].TrimStart().StartsWith("```", StringComparison.Ordinal))
                    code.Add(lines[index++]);
                if (index < lines.Length) index++;
                blocks.Add(new ChatCodeBlock(language, string.Join('\n', code)));
                continue;
            }

            if (index + 1 < lines.Length && IsTableRow(lines[index]) && IsTableSeparator(lines[index + 1]))
            {
                var headers = SplitTableRow(lines[index]);
                var rows = new List<IReadOnlyList<string>>();
                index += 2;
                while (index < lines.Length && IsTableRow(lines[index]))
                    rows.Add(SplitTableRow(lines[index++]));
                blocks.Add(new ChatTableBlock(headers, rows));
                continue;
            }

            if (TryUnorderedItem(trimmed, out _))
            {
                var items = new List<string>();
                while (index < lines.Length && TryUnorderedItem(lines[index].Trim(), out var item))
                {
                    items.Add(item);
                    index++;
                }
                blocks.Add(new ChatListBlock(false, items));
                continue;
            }

            if (TryOrderedItem(trimmed, out _))
            {
                var items = new List<string>();
                while (index < lines.Length && TryOrderedItem(lines[index].Trim(), out var item))
                {
                    items.Add(item);
                    index++;
                }
                blocks.Add(new ChatListBlock(true, items));
                continue;
            }

            var headingLevel = GetHeadingLevel(trimmed);
            if (headingLevel > 0)
            {
                blocks.Add(new ChatHeadingBlock(headingLevel, trimmed[(headingLevel + 1)..].Trim()));
                index++;
                continue;
            }

            var paragraph = new List<string>();
            while (index < lines.Length && !string.IsNullOrWhiteSpace(lines[index]))
            {
                if (paragraph.Count > 0 && StartsStructuredBlock(lines, index)) break;
                paragraph.Add(lines[index].Trim());
                index++;
            }
            blocks.Add(new ChatParagraphBlock(string.Join('\n', paragraph)));
        }

        return blocks;
    }

    private static bool StartsStructuredBlock(string[] lines, int index)
    {
        var line = lines[index].Trim();
        return line.StartsWith("```", StringComparison.Ordinal)
            || GetHeadingLevel(line) > 0
            || TryUnorderedItem(line, out _)
            || TryOrderedItem(line, out _)
            || (index + 1 < lines.Length && IsTableRow(line) && IsTableSeparator(lines[index + 1]));
    }

    private static int GetHeadingLevel(string line)
    {
        var level = 0;
        while (level < line.Length && level < 3 && line[level] == '#') level++;
        return level > 0 && level < line.Length && line[level] == ' ' ? level : 0;
    }

    private static bool TryUnorderedItem(string line, out string item)
    {
        if (line.StartsWith("- ", StringComparison.Ordinal)
            || line.StartsWith("* ", StringComparison.Ordinal)
            || line.StartsWith("• ", StringComparison.Ordinal))
        {
            item = line[2..].Trim();
            return item.Length > 0;
        }
        item = string.Empty;
        return false;
    }

    private static bool TryOrderedItem(string line, out string item)
    {
        var match = OrderedItemRegex().Match(line);
        item = match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        return item.Length > 0;
    }

    private static bool IsTableRow(string line) => SplitTableRow(line).Count >= 2;

    private static bool IsTableSeparator(string line)
    {
        var cells = SplitTableRow(line);
        return cells.Count >= 2 && cells.All(cell => TableSeparatorCellRegex().IsMatch(cell));
    }

    private static IReadOnlyList<string> SplitTableRow(string line) => line.Trim().Trim('|')
        .Split('|', StringSplitOptions.None)
        .Select(cell => cell.Trim())
        .ToList();

    [GeneratedRegex(@"^\d+[.)]\s+(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex OrderedItemRegex();

    [GeneratedRegex(@"^:?-{3,}:?$", RegexOptions.CultureInvariant)]
    private static partial Regex TableSeparatorCellRegex();
}

public abstract record ChatContentBlock;
public sealed record ChatParagraphBlock(string Text) : ChatContentBlock;
public sealed record ChatHeadingBlock(int Level, string Text) : ChatContentBlock;
public sealed record ChatListBlock(bool Ordered, IReadOnlyList<string> Items) : ChatContentBlock;
public sealed record ChatCodeBlock(string Language, string Code) : ChatContentBlock;
public sealed record ChatTableBlock(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows) : ChatContentBlock
{
    public string CellAt(IReadOnlyList<string> row, int index, string fallback = "—") =>
        index >= 0 && index < row.Count && !string.IsNullOrWhiteSpace(row[index])
            ? row[index]
            : fallback;

    public bool IsStatusColumn(int index) =>
        index >= 0 && index < Headers.Count &&
        Headers[index].Contains("status", StringComparison.OrdinalIgnoreCase);

    public ChatStatusTone StatusToneAt(IReadOnlyList<string> row, int index)
    {
        if (!IsStatusColumn(index))
            return ChatStatusTone.None;

        return CellAt(row, index, string.Empty).Trim().ToLowerInvariant() switch
        {
            "completed" or "confirmed" or "available" => ChatStatusTone.Positive,
            "pending" or "draft" or "approved" or "ordered" or "partially received" => ChatStatusTone.Attention,
            "overdue" or "out of stock" or "cancelled" => ChatStatusTone.Critical,
            _ => ChatStatusTone.Neutral
        };
    }
}

public enum ChatStatusTone
{
    None,
    Neutral,
    Positive,
    Attention,
    Critical
}
