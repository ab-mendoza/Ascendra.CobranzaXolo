namespace Ascendra.CobranzaXolo.Application.Recurrence;

public sealed class RecurrenceQuery
{
    public string[]? ClienteUnico { get; init; }
    public string[]? EstadoPlan { get; init; }
    public string[]? Agente { get; init; }
    public string[]? EstatusDespacho { get; init; }
}