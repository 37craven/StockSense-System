using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace StockSense.Infrastructure.Services;

public static class CsvService
{
    public static byte[] ExportToCsv<T>(IEnumerable<T> records, ClassMap<T>? map = null) where T : class
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null
        });

        if (map is not null) csv.Context.RegisterClassMap(map);
        csv.WriteRecords(records);
        writer.Flush();
        return memoryStream.ToArray();
    }

    public static CsvReaderResult<T> ReadCsv<T>(Stream fileStream, ClassMap<T>? map = null) where T : class
    {
        var errors = new List<CsvValidationError>();
        var records = new List<T>();

        try
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8, leaveOpen: true);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                DetectDelimiter = true
            });

            if (map is not null) csv.Context.RegisterClassMap(map);

            var row = 0;
            foreach (var record in csv.GetRecords<T>())
            {
                row++;
                records.Add(record);
            }

            // Re-read to validate
            fileStream.Position = 0;
            using var validateReader = new StreamReader(fileStream, Encoding.UTF8, leaveOpen: true);
            using var validateCsv = new CsvReader(validateReader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                DetectDelimiter = true
            });

            if (map is not null) validateCsv.Context.RegisterClassMap(map);

            row = 0;
            try
            {
                foreach (var _ in validateCsv.GetRecords<T>())
                {
                    row++;
                }
            }
            catch (CsvHelperException ex)
            {
                errors.Add(new CsvValidationError(row + 1, "Row", ex.Message));
            }
        }
        catch (CsvHelperException ex)
        {
            errors.Add(new CsvValidationError(0, "Header", ex.Message));
        }
        catch (Exception ex)
        {
            errors.Add(new CsvValidationError(0, "File", $"Failed to read CSV: {ex.Message}"));
        }

        return new CsvReaderResult<T>(records, errors);
    }
}

public record CsvValidationError(int Row, string Field, string Message);

public record CsvReaderResult<T>(List<T> Records, List<CsvValidationError> Errors) where T : class
{
    public bool IsValid => Errors.Count == 0;
    public int TotalRows => Records.Count;
    public int ErrorCount => Errors.Count;
}
