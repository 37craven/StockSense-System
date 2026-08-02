using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;

namespace StockSense.Web.Controllers;

[ApiController]
[Authorize(Policy = "InventoryStaff")]
[Route("api/motor-compatibility")]
public sealed class MotorCompatibilityController(
    IMotorCompatibilityLookupService lookupService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MotorCompatibilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MotorCompatibilityDto>>> FindExact(
        [FromQuery] string? manufacturer,
        [FromQuery] string? modelName,
        [FromQuery] string? versionName,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manufacturer)
            || string.IsNullOrWhiteSpace(modelName)
            || string.IsNullOrWhiteSpace(versionName)
            || year is null)
        {
            return BadRequest(ApiResponse.Error(
                "Manufacturer, modelName, versionName, and year are required for an exact compatibility lookup."));
        }

        if (manufacturer.Trim().Length > 50
            || modelName.Trim().Length > 100
            || versionName.Trim().Length > 50)
        {
            return BadRequest(ApiResponse.Error("One or more lookup values exceed the supported length."));
        }

        if (year is < 1885 or > 2200)
            return BadRequest(ApiResponse.Error("Year must be between 1885 and 2200."));

        var matches = await lookupService.FindExactAsync(
            new MotorCompatibilityLookupQuery(
                manufacturer.Trim(), modelName.Trim(), versionName.Trim(), year.Value),
            cancellationToken);

        if (matches.Count == 0)
        {
            return NotFound(ApiResponse.Error(
                "No exact compatibility record was found for the specified manufacturer, model, version, and year."));
        }

        return Ok(matches);
    }
}
