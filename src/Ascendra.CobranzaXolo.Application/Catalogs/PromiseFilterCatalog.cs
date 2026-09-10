namespace Ascendra.CobranzaXolo.Application.Catalogs;

public sealed record PromiseFilterCatalog(
    IReadOnlyList<string> IdPromesa,
    IReadOnlyList<string> ClienteUnico,
    IReadOnlyList<string> Agente,
    IReadOnlyList<string> TipoPromesa,
    IReadOnlyList<string> Estatus);