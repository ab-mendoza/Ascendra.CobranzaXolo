namespace Ascendra.CobranzaXolo.Application.Dashboard;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalRecords,
    decimal TotalAmount);