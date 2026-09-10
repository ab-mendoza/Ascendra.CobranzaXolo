using Ascendra.CobranzaXolo.Application.Activity;
using Ascendra.CobranzaXolo.Application.Dashboard;

using Microsoft.AspNetCore.Mvc;

namespace Ascendra.CobranzaXolo.Api.Controllers;

[ApiController]
[Route("api/v1/actividad-promesas")]
public sealed class PromiseActivityController(DashboardReadService dashboard) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PromiseActivityResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PromiseActivityResponse>> Get(
        [FromQuery] PromiseActivityQuery query,
        CancellationToken cancellationToken) =>
        Ok(await dashboard.GetPromiseActivityAsync(query, cancellationToken));

    [HttpGet("detalle")]
    [ProducesResponseType<IReadOnlyList<PromiseActivityDetailItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PromiseActivityDetailItem>>> GetDetail(
        [FromQuery] DateOnly fecha,
        [FromQuery] string agente,
        [FromQuery] int hora,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(agente) || hora is < 0 or > 23)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Parámetros de actividad no válidos.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(await dashboard.GetPromiseActivityDetailAsync(
            fecha, agente, hora, cancellationToken));
    }
}