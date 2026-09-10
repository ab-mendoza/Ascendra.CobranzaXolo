namespace Ascendra.CobranzaXolo.Application.Dashboard;

public sealed record PaymentListItem(
    long Id,
    string Agente,
    DateTime? FechaRecepcion,
    string? ClienteUnico,
    string? Producto,
    decimal RecuperacionPorGestion,
    int? Semana,
    string? DiaSemana,
    string? IdPromesaAsignada,
    string? TipoAsignacion);