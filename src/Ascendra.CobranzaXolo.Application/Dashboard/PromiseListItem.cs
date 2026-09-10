namespace Ascendra.CobranzaXolo.Application.Dashboard;

public sealed record PromiseListItem(
    string IdPromesa,
    string? ClienteUnico,
    string Agente,
    DateTime? FechaPromesa,
    decimal? MontoInicial,
    decimal? MontoSemanal,
    string? TipoPromesa,
    int? NumeroSemanas,
    DateTime? FechaCreacion,
    string Estatus);