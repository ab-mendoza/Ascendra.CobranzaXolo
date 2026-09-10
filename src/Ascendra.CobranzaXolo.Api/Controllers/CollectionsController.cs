using Ascendra.CobranzaXolo.Application.Collections;
using Ascendra.CobranzaXolo.Application.Dashboard;

using Microsoft.AspNetCore.Mvc;

namespace Ascendra.CobranzaXolo.Api.Controllers;

[ApiController]
[Route("api/v1/cobranza")]
public sealed class CollectionsController(DashboardReadService dashboard) : ControllerBase
{
    [HttpGet("heatmap")]
    [ProducesResponseType<CollectionHeatmapResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CollectionHeatmapResponse>> GetHeatmap(
        [FromQuery] CollectionHeatmapQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await dashboard.GetCollectionHeatmapAsync(query, cancellationToken));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Vista de cobranza no válida.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}