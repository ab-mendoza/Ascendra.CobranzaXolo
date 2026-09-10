namespace Ascendra.CobranzaXolo.Application.Activity;

public sealed record PromiseActivityResponse(
    DateOnly Fecha,
    IReadOnlyList<string> Agentes,
    IReadOnlyList<int> Horas,
    IReadOnlyList<PromiseActivityCell> Celdas,
    decimal TotalMontoInicial,
    decimal TotalMontoSemanal);

public sealed record PromiseActivityCell(
    string Agente,
    int Hora,
    int Promesas,
    decimal MontoInicial,
    decimal MontoSemanal);