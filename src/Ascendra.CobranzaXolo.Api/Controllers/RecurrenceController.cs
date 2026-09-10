using Ascendra.CobranzaXolo.Api.Exports;
using Ascendra.CobranzaXolo.Application.Dashboard;
using Ascendra.CobranzaXolo.Application.Recurrence;

using Microsoft.AspNetCore.Mvc;

namespace Ascendra.CobranzaXolo.Api.Controllers;

[ApiController]
[Route("api/v1/recurrencia")]
public sealed class RecurrenceController(DashboardReadService dashboard) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<RecurrenceResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RecurrenceResponse>> Get(
        [FromQuery] RecurrenceQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await dashboard.GetRecurrenceAsync(query, cancellationToken));
    }

    [HttpGet("exportar")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> Export(
        [FromQuery] RecurrenceQuery query,
        CancellationToken cancellationToken)
    {
        var recurrence = await dashboard.GetRecurrenceAsync(query, cancellationToken);
        var fileName = $"ascendra-recurrencia-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
        return File(
            RecurrenceWorkbookExporter.Create(recurrence),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet("detalle")]
    [ProducesResponseType<IReadOnlyList<RecurrenceDetailItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecurrenceDetailItem>>> GetDetail(
        [FromQuery] string clienteUnico,
        [FromQuery] int year,
        [FromQuery] int week,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clienteUnico) || week is < 1 or > 53 || year < 2000)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Parámetros de recurrencia no válidos.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(await dashboard.GetRecurrenceDetailAsync(
            clienteUnico, year, week, cancellationToken));
    }
}