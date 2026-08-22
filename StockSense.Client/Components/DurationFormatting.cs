namespace StockSense.Client.Components;

public static class DurationFormatting
{
    public static string Format(int minutes)
    {
        if (minutes <= 0) return "0m";
        var days = minutes / MinutesPerDay;
        var hours = minutes % MinutesPerDay / 60;
        var mins = minutes % 60;
        var parts = new List<string>(3);
        if (days > 0) parts.Add($"{days}d");
        if (hours > 0) parts.Add($"{hours}h");
        if (mins > 0 || parts.Count == 0) parts.Add($"{mins}m"); // ponytail: always show something
        return string.Join(" ", parts);
    }

    private const int MinutesPerDay = 1440;
}
