namespace Ascendra.CobranzaXolo.Domain.Collections;

public sealed record AttributedPayment(
    SourcePayment Payment,
    string Agent,
    string? LinkedPromiseId,
    string? AssignmentType);