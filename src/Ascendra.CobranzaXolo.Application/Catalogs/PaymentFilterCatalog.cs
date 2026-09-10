namespace Ascendra.CobranzaXolo.Application.Catalogs;

public sealed record PaymentFilterCatalog(
    IReadOnlyList<string> ClienteUnico,
    IReadOnlyList<string> Agente,
    IReadOnlyList<string> Producto,
    IReadOnlyList<string> DiaSemana,
    IReadOnlyList<int> Semana);