using Ascendra.CobranzaXolo.Api.Exports;
using Ascendra.CobranzaXolo.Application.Dashboard;

using Microsoft.AspNetCore.Mvc;

namespace Ascendra.CobranzaXolo.Api.Controllers;

[ApiController]
[Route("api/v1/promesas")]
public sealed class PromisesController(DashboardReadService dashboard) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PromisePageResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PromisePageResult>> Get(
        [FromQuery] PromiseQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await dashboard.GetPromisesAsync(query, cancellationToken));
    }

    [HttpGet("exportar")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> Export(
        [FromQuery] PromiseQuery query,
        CancellationToken cancellationToken)
    {
        var promises = await dashboard.GetPromisesForExportAsync(query, cancellationToken);
        var fileName = $"ascendra-promesas-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
        return File(
            PromisesWorkbookExporter.Create(promises),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}