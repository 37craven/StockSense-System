using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Services;

namespace StockSense.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin, Employee")]
public class SuppliersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SuppliersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<SupplierDto>>> GetSuppliers()
    {
        var suppliers = await _context.Suppliers
            .OrderBy(s => s.Name)
            .Select(s => new SupplierDto(s.Id, s.Name, s.Email, s.MobileNumber))
            .ToListAsync();

        return Ok(suppliers);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportSuppliers()
    {
        var suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
        var records = suppliers.Select(s => new SupplierExportRecord
        {
            Name = s.Name,
            Email = s.Email,
            MobileNumber = s.MobileNumber
        }).ToList();

        var bytes = CsvService.ExportToCsv(records, new SupplierExportMap());
        return File(bytes, "text/csv", $"suppliers_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpPost("import")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportSuppliers(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse.Error("Upload a CSV file."));

        using var stream = file.OpenReadStream();
        var result = CsvService.ReadCsv<SupplierImportRecord>(stream, new SupplierImportMap());

        if (!result.IsValid)
            return BadRequest(new { errors = result.Errors, totalRows = result.TotalRows });

        var preview = new CsvImportPreview<SupplierImportRecord> { TotalRows = result.TotalRows };
        var existingNames = await _context.Suppliers.Select(s => s.Name.Trim().ToUpper()).ToListAsync();

        for (int i = 0; i < result.Records.Count; i++)
        {
            var record = result.Records[i];
            var row = i + 2;
            var rowErrors = new List<CsvImportError>();

            if (string.IsNullOrWhiteSpace(record.Name))
                rowErrors.Add(new CsvImportError { Row = row, Field = "Name", Message = "Name is required." });
            if (existingNames.Contains(record.Name.Trim().ToUpper()) ||
                result.Records.Take(i).Any(r => r.Name.Trim().Equals(record.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                rowErrors.Add(new CsvImportError { Row = row, Field = "Name", Message = $"Supplier \"{record.Name}\" already exists." });

            if (rowErrors.Any())
                preview.Errors.AddRange(rowErrors);
            else
                preview.ValidRows.Add(record);
        }

        return Ok(preview);
    }

    [HttpPost("import/confirm")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ConfirmImportSuppliers([FromBody] List<SupplierImportRecord> records)
    {
        if (records == null || !records.Any())
            return BadRequest(ApiResponse.Error("No valid rows to import."));

        var created = 0;
        foreach (var record in records)
        {
            var supplier = new Supplier
            {
                Name = record.Name.Trim(),
                Email = (record.Email ?? "").Trim(),
                MobileNumber = (record.MobileNumber ?? "").Trim()
            };
            _context.Suppliers.Add(supplier);
            created++;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Successfully imported {created} suppliers.", count = created });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] CreateSupplierDto dto)
    {
        var supplier = new Supplier
        {
            Name = dto.Name.Trim(),
            Email = (dto.Email ?? string.Empty).Trim(),
            MobileNumber = (dto.MobileNumber ?? string.Empty).Trim()
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        var result = new SupplierDto(supplier.Id, supplier.Name, supplier.Email, supplier.MobileNumber);
        return Ok(result);
    }
}
