namespace Ascendra.CobranzaXolo.Domain.Collections;

public sealed record SourcePayment(
    long Id,
    string? ClientId,
    DateTime? ReceivedAt,
    string? Product,
    decimal RecoveryAmount,
    int? Week);