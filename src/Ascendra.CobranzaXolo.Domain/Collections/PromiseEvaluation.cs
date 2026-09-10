namespace Ascendra.CobranzaXolo.Domain.Collections;

public sealed record PromiseEvaluation(
    SourcePromise Promise,
    string Status,
    bool IsDuplicate,
    bool IsActive);