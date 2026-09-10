namespace Ascendra.CobranzaXolo.Application.Dashboard;

public sealed class PaymentQuery
{
    public string[]? ClienteUnico { get; init; }
    public string[]? Agente { get; init; }
    public string[]? Producto { get; init; }
    public string[]? DiaSemana { get; init; }
    public int[]? Semana { get; init; }
    public DateOnly? FechaDesde { get; init; }
    public DateOnly? FechaHasta { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}