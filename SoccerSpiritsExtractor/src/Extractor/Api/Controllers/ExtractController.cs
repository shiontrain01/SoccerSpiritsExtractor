using Extractor.Api;
using Extractor.Application;
using Microsoft.AspNetCore.Mvc;

namespace Extractor.Api.Controllers;

[ApiController]
[Route("extract")]
public sealed class ExtractController : ControllerBase
{
    private readonly IExtractionService _svc;
    private readonly IRunStore _runs;

    public ExtractController(IExtractionService svc, IRunStore runs)
    {
        _svc = svc;
        _runs = runs;
    }

    [HttpPost("from-url")]
    public async Task<ActionResult<ExtractResponse>> FromUrl([FromBody] ExtractFromUrlRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Url))
            return BadRequest("Url required.");

        var run = await _svc.ExtractFromUrlAsync(req.Url, ct);

        return Ok(new ExtractResponse
        {
            RunId = run.RunId,
            Success = run.Success,
            OutputJson = run.OutputJson,
            Error = run.Error
        });
    }

    [HttpGet("runs/{runId}")]
    public async Task<IActionResult> GetRun(string runId, CancellationToken ct)
    {
        var run = await _runs.GetAsync(runId, ct);
        return run is null ? NotFound() : Ok(run);
    }
}
