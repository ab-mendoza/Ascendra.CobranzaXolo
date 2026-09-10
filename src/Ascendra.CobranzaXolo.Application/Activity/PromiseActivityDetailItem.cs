namespace Ascendra.CobranzaXolo.Application.Activity;

public sealed record PromiseActivityDetailItem(
    string IdPromesa,
    string? ClienteUnico,
    string Agente,
    DateTime FechaCreacion,
    DateTime? FechaPromesa,
    string? TipoPromesa,
    string Estatus,
    decimal? MontoInicial,
    decimal? MontoSemanal,
    int? NumeroSemanas);