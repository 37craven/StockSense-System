using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

namespace StockSense.Web.Services;

public interface IHistoricalSalesImporter
{
    Task<SalesImportResult> ImportBundledDatasetAsync(CancellationToken cancellationToken = default);
}

public sealed record SalesImportResult(
    bool AlreadyImported,
    int BatchId,
    int RowsRead,
    int RowsInserted,
    int RowsUpdated,
    int ReportingProductsCreated);

/// <summary>
/// Imports the validated bundled dataset into the reporting model. It deliberately
/// creates no <see cref="Product"/> or <see cref="LiveProductMapping"/> records.
/// </summary>
public sealed class HistoricalSalesCsvImporter : IHistoricalSalesImporter
{
    public const string SourceSystem = "Top100MonthlyProductSales";
    public const string DatasetFileName = "TOP_100_MONTHLY_PRODUCT_SALES_WITH_BRAND_CATEGORY.csv";

    private const int ExpectedRowCount = 4_100;
    private const int ExpectedProductCount = 100;

    private static readonly IReadOnlyDictionary<string, byte> MonthNumbers =
        DateTimeFormatInfo.InvariantInfo.MonthNames
            .Take(12)
            .Select((month, index) => (month, Number: checked((byte)(index + 1))))
            .ToDictionary(item => item.month, item => item.Number, StringComparer.OrdinalIgnoreCase);

    private readonly ApplicationDbContext context;
    private readonly string filePath;
    private readonly ILogger<HistoricalSalesCsvImporter> logger;

    public HistoricalSalesCsvImporter(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        ILogger<HistoricalSalesCsvImporter> logger)
    {
        this.context = context;
        this.logger = logger;
        filePath = Path.Combine(environment.ContentRootPath, "Data", DatasetFileName);
    }

    public async Task<SalesImportResult> ImportBundledDatasetAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The bundled historical sales dataset was not found.", filePath);
        }

        var content = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var contentHash = Convert.ToHexString(SHA256.HashData(content));
        var rows = ParseAndValidate(content);

        var batch = await context.Set<SalesImportBatch>()
            .SingleOrDefaultAsync(
                item => item.SourceSystem == SourceSystem && item.ContentSha256 == contentHash,
                cancellationToken);

        if (batch?.Status == SalesImportStatus.Completed)
        {
            return new SalesImportResult(
                true,
                batch.Id,
                batch.RowsRead,
                batch.RowsInserted,
                batch.RowsUpdated,
                batch.ReportingProductsCreated);
        }

        if (batch is null)
        {
            batch = new SalesImportBatch
            {
                SourceSystem = SourceSystem,
                FileName = DatasetFileName,
                ContentSha256 = contentHash
            };
            context.Add(batch);
        }
        else
        {
            batch.Status = SalesImportStatus.Pending;
            batch.StartedAtUtc = DateTime.UtcNow;
            batch.CompletedAtUtc = null;
            batch.ErrorMessage = null;
            batch.RowsRead = 0;
            batch.RowsInserted = 0;
            batch.RowsUpdated = 0;
            batch.ReportingProductsCreated = 0;
        }

        await context.SaveChangesAsync(cancellationToken);
        var batchId = batch.Id;

        var strategy = context.Database.CreateExecutionStrategy();

        try
        {
            var result = await strategy.ExecuteAsync(async () =>
            {
                // The execution strategy may retry this whole delegate.
                // Clear tracked state so every attempt starts clean.
                context.ChangeTracker.Clear();

                await using var transaction =
                    await context.Database.BeginTransactionAsync(cancellationToken);

                var currentBatch = await context.Set<SalesImportBatch>()
                    .SingleAsync(item => item.Id == batchId, cancellationToken);

                var mappings = await context.Set<HistoricalProductMapping>()
                    .Include(mapping => mapping.ReportingProduct)
                    .Where(mapping => mapping.SourceSystem == SourceSystem)
                    .ToDictionaryAsync(
                        mapping => mapping.ExternalProductKey,
                        StringComparer.Ordinal,
                        cancellationToken);

                var productsCreated = 0;
                foreach (var sourceProduct in rows
                             .GroupBy(row => row.ExternalProductKey, StringComparer.Ordinal)
                             .Select(group => group.First())
                             .OrderBy(row => int.Parse(
                                 row.ExternalProductKey,
                                 CultureInfo.InvariantCulture)))
                {
                    if (mappings.TryGetValue(sourceProduct.ExternalProductKey, out var mapping))
                    {
                        mapping.ReportingProduct.Name = sourceProduct.ProductName;
                        mapping.ReportingProduct.Brand = sourceProduct.Brand;
                        mapping.ReportingProduct.Category = sourceProduct.Category;
                        mapping.ReportingProduct.UpdatedAtUtc = DateTime.UtcNow;
                        continue;
                    }

                    var reportingProduct = new ReportingProduct
                    {
                        Name = sourceProduct.ProductName,
                        Brand = sourceProduct.Brand,
                        Category = sourceProduct.Category
                    };

                    mapping = new HistoricalProductMapping
                    {
                        SourceSystem = SourceSystem,
                        ExternalProductKey = sourceProduct.ExternalProductKey,
                        ReportingProduct = reportingProduct
                    };

                    context.Add(mapping);
                    mappings.Add(mapping.ExternalProductKey, mapping);
                    productsCreated++;
                }

                // Save first so newly created reporting products receive their IDs.
                await context.SaveChangesAsync(cancellationToken);

                var reportingProductIds = mappings.Values
                    .Select(mapping => mapping.ReportingProductId)
                    .ToArray();

                var existingSales = await context.Set<HistoricalMonthlyProductSale>()
                    .Where(sale => reportingProductIds.Contains(sale.ReportingProductId))
                    .ToDictionaryAsync(
                        sale => (sale.ReportingProductId, sale.Year, sale.Month),
                        cancellationToken);

                var inserted = 0;
                var updated = 0;

                foreach (var row in rows)
                {
                    var reportingProductId =
                        mappings[row.ExternalProductKey].ReportingProductId;

                    var key = (reportingProductId, row.Year, row.Month);

                    if (existingSales.TryGetValue(key, out var existingSale))
                    {
                        if (existingSale.QuantitySold != row.QuantitySold ||
                            existingSale.SalesImportBatchId != batchId)
                        {
                            existingSale.QuantitySold = row.QuantitySold;
                            existingSale.SalesImportBatchId = batchId;
                            updated++;
                        }

                        continue;
                    }

                    var sale = new HistoricalMonthlyProductSale
                    {
                        ReportingProductId = reportingProductId,
                        SalesImportBatchId = batchId,
                        Year = row.Year,
                        Month = row.Month,
                        QuantitySold = row.QuantitySold
                    };

                    context.Add(sale);
                    existingSales.Add(key, sale);
                    inserted++;
                }

                currentBatch.RowsRead = rows.Count;
                currentBatch.RowsInserted = inserted;
                currentBatch.RowsUpdated = updated;
                currentBatch.ReportingProductsCreated = productsCreated;
                currentBatch.Status = SalesImportStatus.Completed;
                currentBatch.CompletedAtUtc = DateTime.UtcNow;
                currentBatch.ErrorMessage = null;

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new SalesImportResult(
                    false,
                    batchId,
                    rows.Count,
                    inserted,
                    updated,
                    productsCreated);
            });

            logger.LogInformation(
                "Historical sales import batch {BatchId} completed: {RowsRead} read, {RowsInserted} inserted, {RowsUpdated} updated, {ProductsCreated} reporting products created.",
                result.BatchId,
                result.RowsRead,
                result.RowsInserted,
                result.RowsUpdated,
                result.ReportingProductsCreated);

            return result;
        }
        catch (Exception exception)
        {
            context.ChangeTracker.Clear();

            try
            {
                var failedBatch = await context.Set<SalesImportBatch>()
                    .SingleAsync(item => item.Id == batchId, CancellationToken.None);

                failedBatch.Status = SalesImportStatus.Failed;
                failedBatch.CompletedAtUtc = DateTime.UtcNow;
                failedBatch.ErrorMessage = Truncate(exception.Message, 2_000);

                await context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception statusException)
            {
                logger.LogError(
                    statusException,
                    "Failed to mark historical sales import batch {BatchId} as failed.",
                    batchId);
            }

            logger.LogError(
                exception,
                "Historical sales import batch {BatchId} failed.",
                batchId);

            throw;
        }
    }

    private static IReadOnlyList<HistoricalCsvRow> ParseAndValidate(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var header = reader.ReadLine();
        if (!string.Equals(
                header,
                "ProductID,ProductName,Brand,Category,Year,Month,QuantitySold",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The historical sales CSV has an unexpected header.");
        }

        var rows = new List<HistoricalCsvRow>(ExpectedRowCount);
        var observations = new HashSet<(string ProductKey, short Year, byte Month)>();
        var productMetadata = new Dictionary<string, (string Name, string Brand, string Category)>(StringComparer.Ordinal);
        var lineNumber = 1;

        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseCsvLine(line);
            if (fields.Count != 7 ||
                !int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out var sourceProductId) ||
                sourceProductId <= 0 ||
                !short.TryParse(fields[4], NumberStyles.None, CultureInfo.InvariantCulture, out var year) ||
                year is < 1900 or > 9999 ||
                !MonthNumbers.TryGetValue(fields[5], out var month) ||
                !int.TryParse(fields[6], NumberStyles.None, CultureInfo.InvariantCulture, out var quantitySold) ||
                quantitySold < 0 ||
                string.IsNullOrWhiteSpace(fields[1]) || fields[1].Length > 200 ||
                string.IsNullOrWhiteSpace(fields[2]) || fields[2].Length > 100 ||
                string.IsNullOrWhiteSpace(fields[3]) || fields[3].Length > 100)
            {
                throw new InvalidDataException($"Invalid historical sales data at line {lineNumber}.");
            }

            var productKey = sourceProductId.ToString(CultureInfo.InvariantCulture);
            var metadata = (Name: fields[1], Brand: fields[2], Category: fields[3]);
            if (productMetadata.TryGetValue(productKey, out var existingMetadata) &&
                existingMetadata != metadata)
            {
                throw new InvalidDataException($"Conflicting product metadata at line {lineNumber}.");
            }

            productMetadata[productKey] = metadata;
            if (!observations.Add((productKey, year, month)))
            {
                throw new InvalidDataException($"Duplicate product/year/month at line {lineNumber}.");
            }

            rows.Add(new HistoricalCsvRow(
                productKey,
                fields[1],
                fields[2],
                fields[3],
                year,
                month,
                quantitySold));
        }

        if (rows.Count != ExpectedRowCount || productMetadata.Count != ExpectedProductCount)
        {
            throw new InvalidDataException(
                $"Expected {ExpectedRowCount} rows and {ExpectedProductCount} products, but found {rows.Count} rows and {productMetadata.Count} products.");
        }

        return rows;
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var fields = new List<string>(7);
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
            throw new InvalidDataException("The historical sales CSV contains an unterminated quoted field.");
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed record HistoricalCsvRow(
        string ExternalProductKey,
        string ProductName,
        string Brand,
        string Category,
        short Year,
        byte Month,
        int QuantitySold);
}