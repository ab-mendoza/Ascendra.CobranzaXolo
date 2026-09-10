namespace Ascendra.CobranzaXolo.Application.Dashboard;

public sealed record PromisePageResult(
    IReadOnlyList<PromiseListItem> Items,
    int Page,
    int PageSize,
    int TotalRecords,
    decimal TotalMontoInicial,
    decimal TotalMontoSemanal);