using Microsoft.AspNetCore.Mvc;

namespace Extractor.Api.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get() => Ok(new { status = "ok" });
}
