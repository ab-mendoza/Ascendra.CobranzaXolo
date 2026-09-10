namespace Ascendra.CobranzaXolo.Application.Dashboard;

public sealed class PromiseQuery
{
    public string[]? IdPromesa { get; init; }
    public string[]? ClienteUnico { get; init; }
    public string[]? Agente { get; init; }
    public string[]? TipoPromesa { get; init; }
    public string[]? Estatus { get; init; }
    public DateOnly? FechaPromesaDesde { get; init; }
    public DateOnly? FechaPromesaHasta { get; init; }
    public DateOnly? FechaCreacionDesde { get; init; }
    public DateOnly? FechaCreacionHasta { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}