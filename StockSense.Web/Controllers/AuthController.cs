using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StockSense.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AuthController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus() => Ok();
}
