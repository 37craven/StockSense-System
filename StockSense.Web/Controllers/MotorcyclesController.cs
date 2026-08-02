using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Application.DTOs;
using StockSense.Infrastructure.Data.Repositories;

namespace StockSense.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/motorcycles")]
public sealed class MotorcyclesController(MotorcycleRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MotorcycleOptionDto>>> GetSelectable(CancellationToken cancellationToken)
    {
        var motorcycles = await repository.GetSelectableAsync(cancellationToken);
        return Ok(motorcycles.Select(m => new MotorcycleOptionDto
        {
            Id = m.Id,
            Brand = m.Brand,
            Model = m.Model,
            BaseCC = m.BaseCC
        }).ToList());
    }
}
