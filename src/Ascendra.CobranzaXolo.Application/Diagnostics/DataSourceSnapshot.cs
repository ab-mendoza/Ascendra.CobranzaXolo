namespace Ascendra.CobranzaXolo.Application.Diagnostics;

/// <summary>
/// Small, read-only verification of the three SQL sources used by Ascendra.
/// </summary>
public sealed record DataSourceSnapshot(
    bool IsConfigured,
    bool IsReachable,
    long? Promesas,
    long? Pagos,
    long? RegistrosCartera,
    long? ClientesCarteraActual,
    DateTime? CarteraActualizadaEn,
    string Status);