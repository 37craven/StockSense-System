using StockSense.Domain.Entities;

namespace StockSense.Web.Components.Pages.Admin;

public static class MotorcycleCompatibility
{
    public static (string Brand, string Model, string BaseCC) Key(string brand, string model, string baseCC)
        => (brand.Trim().ToUpperInvariant(), model.Trim().ToUpperInvariant(), baseCC.Trim().ToUpperInvariant());

    public static List<Motorcycle> CollapseDuplicates(IEnumerable<Motorcycle> source)
        => source.GroupBy(m => Key(m.Brand, m.Model, m.BaseCC))
            .Select(group => group.OrderBy(m => m.Id).First())
            .OrderBy(m => m.Brand).ThenBy(m => m.Model).ThenBy(m => m.BaseCC)
            .ToList();
}
