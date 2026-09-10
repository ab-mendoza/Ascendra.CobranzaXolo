using Ascendra.CobranzaXolo.Domain.Collections;

namespace Ascendra.CobranzaXolo.Application.Dashboard;

/// <summary>
/// Read-only source contract. Infrastructure is the only layer that knows SQL Server.
/// </summary>
public interface IDashboardDataSource
{
    Task<IReadOnlyList<SourcePromise>> GetPromisesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SourcePayment>> GetPaymentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortfolioAccount>> GetCurrentPortfolioAsync(CancellationToken cancellationToken = default);
}