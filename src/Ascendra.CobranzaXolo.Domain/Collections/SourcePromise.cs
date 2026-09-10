namespace Ascendra.CobranzaXolo.Domain.Collections;

public sealed record SourcePromise(
    string Id,
    string? ClientId,
    string? Agent,
    DateTime? PromiseDate,
    decimal? InitialAmount,
    decimal? WeeklyAmount,
    string? PromiseType,
    int? NumberOfWeeks,
    DateTime? CreatedAt);