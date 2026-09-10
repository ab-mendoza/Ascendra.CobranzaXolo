namespace Ascendra.CobranzaXolo.Application.Recurrence;

public sealed record RecurrenceDetailItem(
    long PaymentId,
    string ClienteUnico,
    DateTime FechaRecepcion,
    string? Producto,
    decimal RecuperacionPorGestion,
    string EstatusDespacho,
    string EstadoPlan,
    string Agente,
    string? IdPromesaAsignada,
    DateTime? FechaPromesa,
    DateTime? FechaCreacion,
    string? TipoPromesa,
    string? EstatusPromesa,
    decimal? MontoInicial,
    decimal? MontoSemanal,
    int? NumeroSemanas);