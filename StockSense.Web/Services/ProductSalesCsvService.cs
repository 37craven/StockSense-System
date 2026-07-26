using System.Globalization;
using System.Text;

namespace StockSense.Web.Services;

public sealed class ProductSalesCsvService : IProductSalesDatasetService
{
    private const string DatasetFileName = "TOP_100_MONTHLY_PRODUCT_SALES_WITH_BRAND_CATEGORY.csv";

    private static readonly IReadOnlyDictionary<string, int> MonthNumbers =
        DateTimeFormatInfo.InvariantInfo.MonthNames
            .Take(12)
            .Select((month, index) => (month, Number: index + 1))
            .ToDictionary(item => item.month, item => item.Number, StringComparer.OrdinalIgnoreCase);

    private readonly Lazy<Task<ProductSalesDataset>> dataset;

    public ProductSalesCsvService(IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var filePath = Path.Combine(environment.ContentRootPath, "Data", DatasetFileName);
        dataset = new Lazy<Task<ProductSalesDataset>>(
            () => LoadAsync(filePath),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Task<ProductSalesDataset> GetDatasetAsync(CancellationToken cancellationToken = default) =>
        dataset.Value.WaitAsync(cancellationToken);

    public Task<IReadOnlyList<LiveProductOption>> GetLiveProductsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LiveProductOption>>([]);

    public Task<ProductSalesMapping> LinkLiveProductAsync(
        int reportingProductId,
        int liveProductId,
        DateTime? useTransactionsFrom = null,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "Register ProductSalesReportingService to manage historical/live mappings.");

    public Task UnlinkLiveProductAsync(
        int reportingProductId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "Register ProductSalesReportingService to manage historical/live mappings.");

    private static async Task<ProductSalesDataset> LoadAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The bundled product sales dataset could not be found.", filePath);
        }

        var products = new Dictionary<int, ProductBuilder>();
        var years = new SortedSet<int>();
        var uniqueObservations = new HashSet<(int ProductId, int Year, int Month)>();

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);
        using var reader = new StreamReader(stream);

        var header = await reader.ReadLineAsync();
        if (!string.Equals(
                header,
                "ProductID,ProductName,Brand,Category,Year,Month,QuantitySold",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The product sales dataset has an unexpected header.");
        }

        var lineNumber = 1;
        while (await reader.ReadLineAsync() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseCsvLine(line);
            if (fields.Count != 7 ||
                !int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out var productId) ||
                !int.TryParse(fields[4], NumberStyles.None, CultureInfo.InvariantCulture, out var year) ||
                !MonthNumbers.TryGetValue(fields[5], out var month) ||
                !int.TryParse(fields[6], NumberStyles.None, CultureInfo.InvariantCulture, out var quantitySold) ||
                quantitySold < 0)
            {
                throw new InvalidDataException($"Invalid product sales data at line {lineNumber}.");
            }

            if (!uniqueObservations.Add((productId, year, month)))
            {
                throw new InvalidDataException($"Duplicate product sales data at line {lineNumber}.");
            }

            if (!products.TryGetValue(productId, out var product))
            {
                product = new ProductBuilder(productId, fields[1], fields[2], fields[3]);
                products.Add(productId, product);
            }
            else if (!product.HasMetadata(fields[1], fields[2], fields[3]))
            {
                throw new InvalidDataException($"Conflicting product details at line {lineNumber}.");
            }

            product.Sales.Add(new ProductSalesObservation(year, month, quantitySold));
            years.Add(year);
        }

        var productList = products.Values
            .OrderBy(product => product.ProductName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(product => product.ProductId)
            .Select(product => new ProductSalesProduct(
                $"legacy-csv:{product.ProductId}",
                null,
                null,
                product.ProductName,
                product.Brand,
                product.Category,
                ProductSalesSourceStatus.HistoricalOnly,
                GetSuggestedCutover(product.Sales),
                null,
                product.Sales.OrderBy(sale => sale.Year).ThenBy(sale => sale.Month).ToArray()))
            .ToArray();

        return new ProductSalesDataset(productList, years.ToArray());
    }

    private static DateTime? GetSuggestedCutover(IReadOnlyCollection<ProductSalesObservation> sales)
    {
        var latest = sales.OrderByDescending(sale => sale.Year).ThenByDescending(sale => sale.Month).FirstOrDefault();
        return latest is null ? null : new DateTime(latest.Year, latest.Month, 1).AddMonths(1);
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var insideQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (insideQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (character == ',' && !insideQuotes)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        if (insideQuotes)
        {
            throw new InvalidDataException("The product sales dataset contains an unterminated quoted field.");
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }

    private sealed class ProductBuilder(int productId, string productName, string brand, string category)
    {
        public int ProductId { get; } = productId;
        public string ProductName { get; } = productName;
        public string Brand { get; } = brand;
        public string Category { get; } = category;
        public List<ProductSalesObservation> Sales { get; } = [];

        public bool HasMetadata(string candidateName, string candidateBrand, string candidateCategory) =>
            string.Equals(ProductName, candidateName, StringComparison.Ordinal) &&
            string.Equals(Brand, candidateBrand, StringComparison.Ordinal) &&
            string.Equals(Category, candidateCategory, StringComparison.Ordinal);
    }
}
