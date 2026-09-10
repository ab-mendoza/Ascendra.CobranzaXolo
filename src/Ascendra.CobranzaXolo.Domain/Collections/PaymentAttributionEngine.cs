using System.Globalization;

namespace Ascendra.CobranzaXolo.Domain.Collections;

/// <summary>
/// Reproduces the attribution rules used by the current dashboard generator.
/// It is deterministic and contains no database or HTTP dependencies.
/// </summary>
public sealed class PaymentAttributionEngine
{
    private const int ExtraDaysAfterPromiseDate = 1;

    public AttributionResult Evaluate(
        IReadOnlyList<SourcePromise> promises,
        IReadOnlyList<SourcePayment> payments,
        DateOnly cutoffDate)
    {
        var promiseRows = promises
            .Select((promise, index) => new PromiseRow(promise, index))
            .ToList();
        var paymentRows = payments
            .Select(payment => new PaymentRow(payment))
            .ToList();

        var validPromises = promiseRows.Where(row => row.HasValidWindow).ToList();
        var paidInsideWindow = validPromises
            .Where(promise => paymentRows.Any(payment =>
                SameClient(payment.Source.ClientId, promise.Source.ClientId) &&
                payment.Date is not null &&
                payment.Date.Value >= promise.StartDate &&
                payment.Date.Value <= promise.EndDate))
            .Select(promise => promise.Index)
            .ToHashSet();

        MarkDuplicates(validPromises, paidInsideWindow);
        var eligible = validPromises.Where(row => !row.IsDuplicate).ToList();

        var activeAssignments = new Dictionary<long, PromiseRow>();
        var lateAssignments = new Dictionary<long, PromiseRow>();

        foreach (var payment in paymentRows.Where(row => row.Date is not null))
        {
            var activePromise = eligible
                .Where(promise => SameClient(payment.Source.ClientId, promise.Source.ClientId)
                    && payment.Date!.Value >= promise.StartDate
                    && payment.Date.Value <= promise.EndDate)
                .OrderBy(promise => promise.CreatedAt)
                .ThenBy(promise => promise.Index)
                .FirstOrDefault();

            if (activePromise is not null)
            {
                activeAssignments[payment.Source.Id] = activePromise;
            }
        }

        foreach (var payment in paymentRows.Where(row =>
                     row.Date is not null && !activeAssignments.ContainsKey(row.Source.Id)))
        {
            var paymentIsoYear = ISOWeek.GetYear(payment.Date!.Value.ToDateTime(TimeOnly.MinValue));
            var paymentIsoWeek = ISOWeek.GetWeekOfYear(payment.Date.Value.ToDateTime(TimeOnly.MinValue));

            var latePromise = eligible
                .Where(promise => SameClient(payment.Source.ClientId, promise.Source.ClientId)
                    && promise.CreatedAt is not null
                    && ISOWeek.GetYear(promise.CreatedAt.Value) == paymentIsoYear
                    && ISOWeek.GetWeekOfYear(promise.CreatedAt.Value) == paymentIsoWeek
                    && payment.Date.Value > promise.EndDate)
                .OrderBy(promise => promise.CreatedAt)
                .ThenBy(promise => promise.Index)
                .FirstOrDefault();

            if (latePromise is not null)
            {
                lateAssignments[payment.Source.Id] = latePromise;
            }
        }

        var activePromiseIndexes = activeAssignments.Values
            .Select(promise => promise.Index)
            .ToHashSet();

        var evaluatedPromises = promiseRows.Select(row => new PromiseEvaluation(
            row.Source,
            GetStatus(row, activePromiseIndexes, cutoffDate),
            row.IsDuplicate,
            activePromiseIndexes.Contains(row.Index))).ToArray();

        var attributedPayments = paymentRows.Select(payment =>
        {
            if (activeAssignments.TryGetValue(payment.Source.Id, out var activePromise))
            {
                return new AttributedPayment(
                    payment.Source,
                    FormatAgent(activePromise.Source.Agent),
                    activePromise.Source.Id,
                    "Vigente");
            }

            if (lateAssignments.TryGetValue(payment.Source.Id, out var latePromise))
            {
                return new AttributedPayment(
                    payment.Source,
                    FormatAgent(latePromise.Source.Agent),
                    latePromise.Source.Id,
                    "Tardía");
            }

            return new AttributedPayment(payment.Source, "Orgánico", null, null);
        }).ToArray();

        return new AttributionResult(evaluatedPromises, attributedPayments);
    }

    public static string FormatAgent(string? agent)
    {
        if (string.IsNullOrWhiteSpace(agent))
        {
            return "Orgánico";
        }

        var words = agent.Trim().ToLowerInvariant().Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var textInfo = CultureInfo.GetCultureInfo("es-MX").TextInfo;
        var formatted = words.Take(3).Select(word => textInfo.ToTitleCase(word));
        return string.Join(' ', formatted);
    }

    private static void MarkDuplicates(
        IEnumerable<PromiseRow> validPromises,
        ISet<int> paidInsideWindow)
    {
        foreach (var group in validPromises
                     .Where(row => row.CreatedAt is not null && row.Source.ClientId is not null)
                     .GroupBy(row => NormalizeClient(row.Source.ClientId)!))
        {
            var originals = new List<PromiseRow>();
            foreach (var promise in group.OrderBy(row => row.CreatedAt).ThenBy(row => row.Source.Id)
                         .ThenBy(row => row.Index))
            {
                promise.IsDuplicate = originals.Any(original =>
                    DateOnly.FromDateTime(promise.CreatedAt!.Value) >= original.StartDate &&
                    DateOnly.FromDateTime(promise.CreatedAt.Value) <= original.EndDate &&
                    !paidInsideWindow.Contains(original.Index));

                if (!promise.IsDuplicate)
                {
                    originals.Add(promise);
                }
            }
        }
    }

    private static string GetStatus(
        PromiseRow row,
        ISet<int> activePromiseIndexes,
        DateOnly cutoffDate)
    {
        if (row.IsDuplicate)
        {
            return "Duplicada";
        }

        if (activePromiseIndexes.Contains(row.Index))
        {
            return "Activa";
        }

        return row.HasValidWindow && cutoffDate >= row.StartDate && cutoffDate <= row.EndDate
            ? "Vigente"
            : "Incumplida";
    }

    private static bool SameClient(string? left, string? right) =>
        string.Equals(NormalizeClient(left), NormalizeClient(right), StringComparison.Ordinal);

    private static string? NormalizeClient(string? clientId) =>
        string.IsNullOrWhiteSpace(clientId) ? null : clientId.Trim();

    private sealed class PromiseRow
    {
        public PromiseRow(SourcePromise source, int index)
        {
            Source = source;
            Index = index;
            CreatedAt = source.CreatedAt;
            StartDate = source.CreatedAt is null
                ? DateOnly.MinValue
                : DateOnly.FromDateTime(source.CreatedAt.Value);
            EndDate = source.PromiseDate is null
                ? DateOnly.MinValue
                : DateOnly.FromDateTime(source.PromiseDate.Value).AddDays(ExtraDaysAfterPromiseDate);
        }

        public SourcePromise Source { get; }
        public int Index { get; }
        public DateTime? CreatedAt { get; }
        public DateOnly StartDate { get; }
        public DateOnly EndDate { get; }
        public bool IsDuplicate { get; set; }
        public bool HasValidWindow =>
            Source.ClientId is not null && Source.PromiseDate is not null && CreatedAt is not null
            && StartDate <= EndDate;
    }

    private sealed class PaymentRow
    {
        public PaymentRow(SourcePayment source)
        {
            Source = source;
        }

        public SourcePayment Source { get; }
        public DateOnly? Date => Source.ReceivedAt is null
            ? null
            : DateOnly.FromDateTime(Source.ReceivedAt.Value);
    }
}

public sealed record AttributionResult(
    IReadOnlyList<PromiseEvaluation> Promises,
    IReadOnlyList<AttributedPayment> Payments);