namespace Ascendra.CobranzaXolo.Domain.Plans;

/// <summary>
/// Translates the source value without changing the value stored in SQL Server.
/// </summary>
public static class PlanStatus
{
    public const string NoPlanSourceValue = "N/A";
    public const string NoPlanDisplayValue = "Sin plan";

    public static string ToDisplay(string? sourceValue) =>
        string.Equals(sourceValue?.Trim(), NoPlanSourceValue, StringComparison.OrdinalIgnoreCase)
            ? NoPlanDisplayValue
            : string.IsNullOrWhiteSpace(sourceValue)
                ? NoPlanDisplayValue
                : sourceValue.Trim();
}