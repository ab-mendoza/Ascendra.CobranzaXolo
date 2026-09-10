namespace Ascendra.CobranzaXolo.Application.Catalogs;

public sealed record RecurrenceFilterCatalog(
    IReadOnlyList<string> ClienteUnico,
    IReadOnlyList<string> EstadoPlan,
    IReadOnlyList<string> Agente,
    IReadOnlyList<string> EstatusDespacho);