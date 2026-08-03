using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

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
