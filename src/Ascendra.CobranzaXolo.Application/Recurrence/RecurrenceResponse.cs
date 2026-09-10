namespace Ascendra.CobranzaXolo.Application.Recurrence;

public sealed record RecurrenceResponse(
    IReadOnlyList<RecurrencePeriod> Periods,
    IReadOnlyList<RecurrenceRow> Rows,
    decimal TotalRecovery);

public sealed record RecurrencePeriod(string Id, string Label, int Year, int Week);

public sealed record RecurrenceRow(
    string ClienteUnico,
    string EstatusDespacho,
    string Agente,
    string EstadoPlan,
    IReadOnlyDictionary<string, decimal?> Payments);