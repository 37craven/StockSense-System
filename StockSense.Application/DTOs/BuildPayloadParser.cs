using System.Text.Json;

namespace StockSense.Application.DTOs;

public static class BuildPayloadParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<int> ParseProductIds(string? selectedPartsJson)
    {
        if (string.IsNullOrWhiteSpace(selectedPartsJson))
            throw new ArgumentException("Select at least one inventory product.", nameof(selectedPartsJson));

        List<BuildPartDto> parts;
        try
        {
            parts = JsonSerializer.Deserialize<List<BuildPartDto>>(selectedPartsJson, JsonOptions) ?? [];
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Selected-parts data is not valid JSON.", nameof(selectedPartsJson), exception);
        }

        var ids = parts.Where(part => part.Id > 0).Select(part => part.Id).ToArray();
        if (ids.Length == 0)
            throw new ArgumentException("Select at least one inventory product.", nameof(selectedPartsJson));
        if (ids.Distinct().Count() != ids.Length)
            throw new ArgumentException("A product cannot be selected more than once.", nameof(selectedPartsJson));

        return ids;
    }
}
