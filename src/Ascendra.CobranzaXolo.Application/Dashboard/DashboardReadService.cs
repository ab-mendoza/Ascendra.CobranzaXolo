using System.Globalization;

using Ascendra.CobranzaXolo.Application.Activity;
using Ascendra.CobranzaXolo.Application.Catalogs;
using Ascendra.CobranzaXolo.Application.Collections;
using Ascendra.CobranzaXolo.Application.Recurrence;
using Ascendra.CobranzaXolo.Domain.Collections;
using Ascendra.CobranzaXolo.Domain.Plans;

namespace Ascendra.CobranzaXolo.Application.Dashboard;

public sealed class DashboardReadService(
    IDashboardDataSource source,
    PaymentAttributionEngine attributionEngine)
{
    public async Task<PromisePageResult> GetPromisesAsync(
        PromiseQuery query,
        CancellationToken cancellationToken = default)
    {
        var filtered = await GetFilteredPromisesAsync(query, cancellationToken);

        var normalizedPage = Math.Max(query.Page, 1);
        var normalizedPageSize = Math.Clamp(query.PageSize, 1, 200);
        return new PromisePageResult(
            filtered.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).ToArray(),
            normalizedPage,
            normalizedPageSize,
            filtered.Count,
            filtered.Sum(item => item.MontoInicial ?? 0m),
            filtered.Sum(item => item.MontoSemanal ?? 0m));
    }

    public async Task<IReadOnlyList<PromiseListItem>> GetPromisesForExportAsync(
        PromiseQuery query,
        CancellationToken cancellationToken = default) =>
        await GetFilteredPromisesAsync(query, cancellationToken);

    public async Task<PagedResult<PaymentListItem>> GetPaymentsAsync(
        PaymentQuery query,
        CancellationToken cancellationToken = default)
    {
        var analysis = await GetAnalysisAsync(cancellationToken);
        var filtered = analysis.Payments
            .Where(payment => payment.Payment.RecoveryAmount != 0m)
            .Select(payment => new PaymentListItem(
                payment.Payment.Id,
                payment.Agent,
                payment.Payment.ReceivedAt,
                Clean(payment.Payment.ClientId),
                Clean(payment.Payment.Product),
                payment.Payment.RecoveryAmount,
                payment.Payment.Week,
                DayName(payment.Payment.ReceivedAt),
                payment.LinkedPromiseId,
                payment.AssignmentType))
            .Where(item => Matches(item.ClienteUnico, query.ClienteUnico))
            .Where(item => Matches(item.Agente, query.Agente))
            .Where(item => Matches(item.Producto, query.Producto))
            .Where(item => Matches(item.DiaSemana, query.DiaSemana))
            .Where(item => query.Semana is null || !query.Semana.Any() || item.Semana is not null && query.Semana.Contains(item.Semana.Value))
            .Where(item => InRange(item.FechaRecepcion, query.FechaDesde, query.FechaHasta))
            .OrderByDescending(item => item.FechaRecepcion)
            .ThenByDescending(item => item.Id)
            .ToList();

        return Page(filtered, query.Page, query.PageSize, filtered.Sum(item => item.RecuperacionPorGestion));
    }

    public async Task<CollectionHeatmapResponse> GetCollectionHeatmapAsync(
        CollectionHeatmapQuery query,
        CancellationToken cancellationToken = default)
    {
        var analysis = await GetAnalysisAsync(cancellationToken);
        var payments = analysis.Payments
            .Where(payment => payment.Payment.RecoveryAmount != 0m && payment.Payment.ReceivedAt is not null)
            .ToArray();
        var view = query.View.Trim().ToLowerInvariant();

        if (view == "weekly-comparison")
        {
            var cells = payments
                .Where(payment => !string.Equals(payment.Agent, "Orgánico", StringComparison.OrdinalIgnoreCase))
                .Where(payment => payment.Payment.Week is >= 1 and <= 53)
                .GroupBy(payment => new { payment.Agent, Week = payment.Payment.Week!.Value })
                .Select(group => new CollectionHeatmapCell(
                    group.Key.Agent,
                    $"Semana {group.Key.Week}",
                    group.Sum(item => item.Payment.RecoveryAmount)))
                .ToArray();
            var columns = cells.Select(cell => cell.Column)
                .Distinct().OrderBy(label => int.Parse(label[7..], CultureInfo.InvariantCulture)).ToArray();
            return Matrix(view, null, cells, columns);
        }

        var week = query.Week ?? payments
            .Where(payment => payment.Payment.Week is not null)
            .Select(payment => payment.Payment.Week!.Value)
            .DefaultIfEmpty()
            .Max();
        var weeklyPayments = payments.Where(payment => payment.Payment.Week == week).ToArray();

        return view switch
        {
            "agent-day" => Matrix(
                view,
                week,
                weeklyPayments
                    .GroupBy(payment => new { payment.Agent, Day = DayName(payment.Payment.ReceivedAt)! })
                    .Select(group => new CollectionHeatmapCell(
                        group.Key.Agent, group.Key.Day, group.Sum(item => item.Payment.RecoveryAmount)))
                    .ToArray(),
                ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"]),
            "agent-product" => Matrix(
                view,
                week,
                weeklyPayments
                    .Where(payment => !string.IsNullOrWhiteSpace(payment.Payment.Product))
                    .GroupBy(payment => new { payment.Agent, Product = Clean(payment.Payment.Product)! })
                    .Select(group => new CollectionHeatmapCell(
                        group.Key.Agent, group.Key.Product, group.Sum(item => item.Payment.RecoveryAmount)))
                    .ToArray(),
                weeklyPayments.Select(payment => Clean(payment.Payment.Product))
                    .Where(product => product is not null).Cast<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(product => product).ToArray()),
            _ => throw new ArgumentOutOfRangeException(nameof(query.View),
                "View debe ser agent-day, agent-product o weekly-comparison.")
        };
    }

    public async Task<PromiseFilterCatalog> GetPromiseCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var analysis = await GetAnalysisAsync(cancellationToken);
        var promises = analysis.Promises.Select(item => new
        {
            Id = item.Promise.Id,
            Client = Clean(item.Promise.ClientId),
            Agent = PaymentAttributionEngine.FormatAgent(item.Promise.Agent),
            Type = Clean(item.Promise.PromiseType),
            item.Status
        }).ToArray();
        return new PromiseFilterCatalog(
            OrderedText(promises.Select(item => item.Id)),
            OrderedText(promises.Select(item => item.Client)),
            OrderedText(promises.Select(item => item.Agent)),
            OrderedText(promises.Select(item => item.Type)),
            OrderedText(promises.Select(item => item.Status)));
    }

    public async Task<PaymentFilterCatalog> GetPaymentCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var analysis = await GetAnalysisAsync(cancellationToken);
        var payments = analysis.Payments.Where(item => item.Payment.RecoveryAmount != 0m).ToArray();
        var days = new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
        return new PaymentFilterCatalog(
            OrderedText(payments.Select(item => Clean(item.Payment.ClientId))),
            OrderedText(payments.Select(item => item.Agent)),
            OrderedText(payments.Select(item => Clean(item.Payment.Product))),
            days.Where(day => payments.Any(item => DayName(item.Payment.ReceivedAt) == day)).ToArray(),
            payments.Where(item => item.Payment.Week is not null).Select(item => item.Payment.Week!.Value)
                .Distinct().OrderByDescending(week => week).ToArray());
    }

    public async Task<RecurrenceFilterCatalog> GetRecurrenceCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var analysis = await GetAnalysisAsync(cancellationToken);
        var portfolio = await source.GetCurrentPortfolioAsync(cancellationToken);
        var entries = BuildRecurrenceEntries(analysis, portfolio);
        var agents = LatestAgents(entries);
        return new RecurrenceFilterCatalog(
            OrderedText(entries.Select(item => item.ClientId)),
            OrderedText(entries.Select(item => item.PlanStatus)),
            OrderedText(agents.Values),
            OrderedText(entries.Select(item => item.DispatchStatus)));
    }

    public async Task<RecurrenceResponse> GetRecurrenceAsync(
        RecurrenceQuery query,
        CancellationToken cancellationToken = default)
    {
        var analysis = await GetAnalysisAsync(cancellationToken);
        var portfolio = await source.GetCurrentPortfolioAsync(cancellationToken);
        var allEntries = BuildRecurrenceEntries(analysis, portfolio);
        var periods = allEntries.Select(entry => entry.Period).DistinctBy(period => period.Id)
            .OrderBy(period => period.Year).ThenBy(period => period.Week).ToArray();
        var agents = LatestAgents(allEntries);
        var paid = allEntries.Where(entry => entry.Payment.Payment.RecoveryAmount != 0m).ToArray();

        var filtered = paid.Where(entry =>
                Matches(entry.ClientId, query.ClienteUnico)
                && Matches(entry.PlanStatus, query.EstadoPlan)
                && Matches(agents[entry.ClientId], query.Agente)
                && Matches(entry.DispatchStatus, query.EstatusDespacho))
            .ToArray();

        var rows = filtered.GroupBy(entry => entry.ClientId).Select(group =>
        {
            var amounts = group.GroupBy(entry => entry.Period.Id)
                .ToDictionary(item => item.Key, item => (decimal?)item.Sum(record => record.Payment.Payment.RecoveryAmount));
            var completedAmounts = periods.ToDictionary(
                period => period.Id,
                period => amounts.GetValueOrDefault(period.Id));
            return new RecurrenceRow(
                group.Key,
                group.First().DispatchStatus,
                agents[group.Key],
                group.First().PlanStatus,
                completedAmounts);
        })
        .OrderByDescending(row => row.Payments.Count(amount => amount.Value is not null))
        .ThenByDescending(row => row.Payments.Values.Sum(amount => amount ?? 0m))
        .ThenBy(row => row.ClienteUnico)
        .ToArray();

        return new RecurrenceResponse(
            periods,
            rows,
            filtered.Sum(entry => entry.Payment.Payment.RecoveryAmount));
    }

    public async Task<IReadOnlyList<RecurrenceDetailItem>> GetRecurrenceDetailAsync(
        string clientId,
        int year,
        int week,
        CancellationToken cancellationToken = default)
    {
        var analysis = await GetAnalysisAsync(cancellationToken);
        var portfolio = await source.GetCurrentPortfolioAsync(cancellationToken);
        var promiseById = analysis.Promises.ToDictionary(item => item.Promise.Id, StringComparer.OrdinalIgnoreCase);

        return BuildRecurrenceEntries(analysis, portfolio)
            .Where(entry => string.Equals(entry.ClientId, NormalizeClient(clientId), StringComparison.OrdinalIgnoreCase))
            .Where(entry => entry.Period.Year == year && entry.Period.Week == week)
            .Where(entry => entry.Payment.Payment.RecoveryAmount != 0m)
            .OrderBy(entry => entry.Payment.Payment.ReceivedAt)
            .Select(entry =>
            {
                promiseById.TryGetValue(entry.Payment.LinkedPromiseId ?? string.Empty, out var promise);
                return new RecurrenceDetailItem(
                    entry.Payment.Payment.Id,
                    entry.ClientId,
                    entry.Payment.Payment.ReceivedAt!.Value,
                    Clean(entry.Payment.Payment.Product),
                    entry.Payment.Payment.RecoveryAmount,
                    entry.DispatchStatus,
                    entry.PlanStatus,
                    entry.Payment.Agent,
                    entry.Payment.LinkedPromiseId,
                    promise?.Promise.PromiseDate,
                    promise?.Promise.CreatedAt,
                    Clean(promise?.Promise.PromiseType),
                    promise?.Status,
                    promise?.Promise.InitialAmount,
                    promise?.Promise.WeeklyAmount,
                    promise?.Promise.NumberOfWeeks);
            }).ToArray();
    }

    public async Task<PromiseActivityResponse> GetPromiseActivityAsync(
        PromiseActivityQuery query,
        CancellationToken cancellationToken = default)
    {
        var analysis = await GetAnalysisAsync(cancellationToken);
        var datedPromises = analysis.Promises
            .Where(item => item.Promise.CreatedAt is not null)
            .ToArray();
        var date = query.Fecha ?? datedPromises
            .Select(item => DateOnly.FromDateTime(item.Promise.CreatedAt!.Value))
            .DefaultIfEmpty(MexicoToday())
            .Max();
        var selected = datedPromises
            .Where(item => DateOnly.FromDateTime(item.Promise.CreatedAt!.Value) == date)
            .ToArray();
        var cells = selected
            .GroupBy(item => new
            {
                Agent = PaymentAttributionEngine.FormatAgent(item.Promise.Agent),
                Hour = item.Promise.CreatedAt!.Value.Hour
            })
            .Select(group => new PromiseActivityCell(
                group.Key.Agent,
                group.Key.Hour,
                group.Count(),
                group.Sum(item => item.Promise.InitialAmount ?? 0m),
                group.Sum(item => item.Promise.WeeklyAmount ?? 0m)))
            .ToArray();

        return new PromiseActivityResponse(
            date,
            cells.Select(cell => cell.Agente).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(agent => agent).ToArray(),
            Enumerable.Range(0, 24).ToArray(),
            cells,
            cells.Sum(cell => cell.MontoInicial),
            cells.Sum(cell => cell.MontoSemanal));
    }

    public async Task<IReadOnlyList<PromiseActivityDetailItem>> GetPromiseActivityDetailAsync(
        DateOnly date,
        string agent,
        int hour,
        CancellationToken cancellationToken = default)
    {
        var analysis = await GetAnalysisAsync(cancellationToken);
        return analysis.Promises
            .Where(item => item.Promise.CreatedAt is not null)
            .Where(item => DateOnly.FromDateTime(item.Promise.CreatedAt!.Value) == date)
            .Where(item => item.Promise.CreatedAt!.Value.Hour == hour)
            .Where(item => string.Equals(PaymentAttributionEngine.FormatAgent(item.Promise.Agent), agent.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Promise.CreatedAt)
            .Select(item => new PromiseActivityDetailItem(
                item.Promise.Id,
                Clean(item.Promise.ClientId),
                PaymentAttributionEngine.FormatAgent(item.Promise.Agent),
                item.Promise.CreatedAt!.Value,
                item.Promise.PromiseDate,
                Clean(item.Promise.PromiseType),
                item.Status,
                item.Promise.InitialAmount,
                item.Promise.WeeklyAmount,
                item.Promise.NumberOfWeeks))
            .ToArray();
    }

    private async Task<AttributionResult> GetAnalysisAsync(CancellationToken cancellationToken)
    {
        var promises = await source.GetPromisesAsync(cancellationToken);
        var payments = await source.GetPaymentsAsync(cancellationToken);
        return attributionEngine.Evaluate(promises, payments, MexicoToday());
    }

    private async Task<List<PromiseListItem>> GetFilteredPromisesAsync(
        PromiseQuery query,
        CancellationToken cancellationToken)
    {
        var analysis = await GetAnalysisAsync(cancellationToken);
        return analysis.Promises
            .Select(promise => new PromiseListItem(
                promise.Promise.Id,
                Clean(promise.Promise.ClientId),
                PaymentAttributionEngine.FormatAgent(promise.Promise.Agent),
                promise.Promise.PromiseDate,
                promise.Promise.InitialAmount,
                promise.Promise.WeeklyAmount,
                Clean(promise.Promise.PromiseType),
                promise.Promise.NumberOfWeeks,
                promise.Promise.CreatedAt,
                promise.Status))
            .Where(item => Matches(item.IdPromesa, query.IdPromesa))
            .Where(item => Matches(item.ClienteUnico, query.ClienteUnico))
            .Where(item => Matches(item.Agente, query.Agente))
            .Where(item => Matches(item.TipoPromesa, query.TipoPromesa))
            .Where(item => Matches(item.Estatus, query.Estatus))
            .Where(item => InRange(item.FechaPromesa, query.FechaPromesaDesde, query.FechaPromesaHasta))
            .Where(item => InRange(item.FechaCreacion, query.FechaCreacionDesde, query.FechaCreacionHasta))
            .OrderByDescending(item => item.FechaPromesa)
            .ThenByDescending(item => item.FechaCreacion)
            .ThenBy(item => item.IdPromesa)
            .ToList();
    }

    private static CollectionHeatmapResponse Matrix(
        string view,
        int? week,
        IReadOnlyList<CollectionHeatmapCell> cells,
        IReadOnlyList<string> columns)
    {
        var rows = cells.GroupBy(cell => cell.Row)
            .OrderByDescending(group => group.Sum(cell => cell.Amount))
            .ThenBy(group => group.Key)
            .Select(group => group.Key).ToArray();
        return new CollectionHeatmapResponse(
            view,
            week,
            rows,
            columns,
            cells,
            cells.Sum(cell => cell.Amount));
    }

    private static IReadOnlyList<RecurrenceEntry> BuildRecurrenceEntries(
        AttributionResult analysis,
        IReadOnlyList<PortfolioAccount> portfolio)
    {
        var portfolioByClient = portfolio
            .Where(account => NormalizeClient(account.ClientId) is not null)
            .GroupBy(account => NormalizeClient(account.ClientId)!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => PlanStatus.ToDisplay(group.First().PlanStatus),
                StringComparer.OrdinalIgnoreCase);

        return analysis.Payments
            .Where(payment => NormalizeClient(payment.Payment.ClientId) is not null)
            .Where(payment => payment.Payment.ReceivedAt is not null)
            .Where(payment => payment.Payment.Week is >= 1 and <= 53)
            .Select(payment =>
            {
                var clientId = NormalizeClient(payment.Payment.ClientId)!;
                var date = payment.Payment.ReceivedAt!.Value;
                var year = ISOWeek.GetYear(date);
                var week = payment.Payment.Week!.Value;
                var assigned = portfolioByClient.TryGetValue(clientId, out var planStatus);
                return new RecurrenceEntry(
                    clientId,
                    assigned ? "Activo" : "Inactivo",
                    assigned ? planStatus! : "Sin información de plan",
                    new RecurrencePeriod($"week_{year}_{week:D2}", $"Semana {week} · {year}", year, week),
                    payment);
            }).ToArray();
    }

    private static IReadOnlyDictionary<string, string> LatestAgents(IReadOnlyList<RecurrenceEntry> entries) =>
        entries.GroupBy(entry => entry.ClientId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group =>
            {
                var latest = group.OrderByDescending(entry => entry.Period.Year)
                    .ThenByDescending(entry => entry.Period.Week)
                    .First().Period;
                var values = group.Where(entry => entry.Period.Id == latest.Id)
                    .Select(entry => entry.Payment.Agent).Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(agent => agent).ToArray();
                return values.Length switch
                {
                    0 => "Orgánico",
                    1 => values[0],
                    2 => string.Join(" / ", values),
                    _ => $"Múltiples ({values.Length})"
                };
            }, StringComparer.OrdinalIgnoreCase);

    private sealed record RecurrenceEntry(
        string ClientId,
        string DispatchStatus,
        string PlanStatus,
        RecurrencePeriod Period,
        AttributedPayment Payment);

    private static PagedResult<T> Page<T>(IReadOnlyList<T> records, int page, int pageSize, decimal total)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 200);
        return new PagedResult<T>(
            records.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).ToArray(),
            normalizedPage,
            normalizedPageSize,
            records.Count,
            total);
    }

    private static bool Matches(string? value, IEnumerable<string>? selected) =>
        selected is null || !selected.Any() || selected.Any(choice =>
            string.Equals(value?.Trim(), choice?.Trim(), StringComparison.OrdinalIgnoreCase));

    private static bool InRange(DateTime? value, DateOnly? from, DateOnly? to)
    {
        if (value is null)
        {
            return from is null && to is null;
        }

        var date = DateOnly.FromDateTime(value.Value);
        return (from is null || date >= from) && (to is null || date <= to);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeClient(string? value) => Clean(value);

    private static IReadOnlyList<string> OrderedText(IEnumerable<string?> values) => values
        .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToArray();

    private static string? DayName(DateTime? date) => date is null ? null : date.Value.DayOfWeek switch
    {
        DayOfWeek.Monday => "Lunes",
        DayOfWeek.Tuesday => "Martes",
        DayOfWeek.Wednesday => "Miércoles",
        DayOfWeek.Thursday => "Jueves",
        DayOfWeek.Friday => "Viernes",
        DayOfWeek.Saturday => "Sábado",
        DayOfWeek.Sunday => "Domingo",
        _ => null
    };

    private static DateOnly MexicoToday()
    {
        foreach (var timeZoneId in new[] { "America/Mexico_City", "Central Standard Time (Mexico)" })
        {
            try
            {
                return DateOnly.FromDateTime(
                    TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, timeZoneId).DateTime);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the platform-specific identifier next.
            }
        }

        return DateOnly.FromDateTime(DateTime.Now);
    }
}