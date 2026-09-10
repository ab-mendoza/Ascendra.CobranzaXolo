using Ascendra.CobranzaXolo.Application.Dashboard;

using Microsoft.AspNetCore.Mvc;

namespace Ascendra.CobranzaXolo.Api.Controllers;

[ApiController]
[Route("api/v1/pagos")]
public sealed class PaymentsController(DashboardReadService dashboard) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<PaymentListItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PaymentListItem>>> Get(
        [FromQuery] PaymentQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await dashboard.GetPaymentsAsync(query, cancellationToken));
    }
}