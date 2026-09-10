using Ascendra.CobranzaXolo.Application.Catalogs;
using Ascendra.CobranzaXolo.Application.Dashboard;

using Microsoft.AspNetCore.Mvc;

namespace Ascendra.CobranzaXolo.Api.Controllers;

[ApiController]
[Route("api/v1/catalogos")]
public sealed class CatalogsController(DashboardReadService dashboard) : ControllerBase
{
    [HttpGet("promesas")]
    [ProducesResponseType<PromiseFilterCatalog>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PromiseFilterCatalog>> Promises(CancellationToken cancellationToken) =>
        Ok(await dashboard.GetPromiseCatalogAsync(cancellationToken));

    [HttpGet("pagos")]
    [ProducesResponseType<PaymentFilterCatalog>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentFilterCatalog>> Payments(CancellationToken cancellationToken) =>
        Ok(await dashboard.GetPaymentCatalogAsync(cancellationToken));

    [HttpGet("recurrencia")]
    [ProducesResponseType<RecurrenceFilterCatalog>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RecurrenceFilterCatalog>> Recurrence(CancellationToken cancellationToken) =>
        Ok(await dashboard.GetRecurrenceCatalogAsync(cancellationToken));
}