using Ascendra.CobranzaXolo.Application.Diagnostics;

using Microsoft.AspNetCore.Mvc;

namespace Ascendra.CobranzaXolo.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController(IDataSourceDiagnostics diagnostics) : ControllerBase
{
    [HttpGet("data-sources")]
    [ProducesResponseType<DataSourceSnapshot>(StatusCodes.Status200OK)]
    [ProducesResponseType<DataSourceSnapshot>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<DataSourceSnapshot>> GetDataSources(
        CancellationToken cancellationToken)
    {
        var snapshot = await diagnostics.GetSnapshotAsync(cancellationToken);

        return snapshot.IsReachable
            ? Ok(snapshot)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, snapshot);
    }
}