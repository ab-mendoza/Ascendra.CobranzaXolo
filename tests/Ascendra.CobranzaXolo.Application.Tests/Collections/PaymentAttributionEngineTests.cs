using Ascendra.CobranzaXolo.Domain.Collections;

namespace Ascendra.CobranzaXolo.Application.Tests.Collections;

public sealed class PaymentAttributionEngineTests
{
    private readonly PaymentAttributionEngine _engine = new();

    [Fact]
    public void GivesPriorityToTheActivePromiseOverAnExpiredPromise()
    {
        var promises = new[]
        {
            Promise("old", "A-1", "Ana López", "2026-08-01", "2026-08-02"),
            Promise("current", "A-1", "Bruno Díaz", "2026-09-01", "2026-09-05")
        };
        var payments = new[] { Payment(1, "A-1", "2026-09-02", 500m) };

        var result = _engine.Evaluate(promises, payments, new DateOnly(2026, 9, 10));

        var payment = Assert.Single(result.Payments);
        Assert.Equal("current", payment.LinkedPromiseId);
        Assert.Equal("Bruno Díaz", payment.Agent);
        Assert.Equal("Vigente", payment.AssignmentType);
        Assert.Equal("Activa", result.Promises.Single(item => item.Promise.Id == "current").Status);
    }

    [Fact]
    public void AssignsLatePaymentToTheFirstEligiblePromiseInTheSameIsoWeek()
    {
        var promises = new[]
        {
            Promise("p-1", "A-1", "Kevin Diaz", "2026-09-01", "2026-09-01")
        };
        var payments = new[] { Payment(1, "A-1", "2026-09-04", 800m) };

        var result = _engine.Evaluate(promises, payments, new DateOnly(2026, 9, 10));

        var payment = Assert.Single(result.Payments);
        Assert.Equal("p-1", payment.LinkedPromiseId);
        Assert.Equal("Kevin Diaz", payment.Agent);
        Assert.Equal("Tardía", payment.AssignmentType);
        Assert.Equal("Incumplida", Assert.Single(result.Promises).Status);
    }

    private static SourcePromise Promise(
        string id,
        string clientId,
        string agent,
        string createdAt,
        string promiseDate) => new(
        id,
        clientId,
        agent,
        DateTime.Parse(promiseDate),
        1_000m,
        500m,
        "Plan de Pago",
        2,
        DateTime.Parse(createdAt));

    private static SourcePayment Payment(long id, string clientId, string receivedAt, decimal amount) =>
        new(id, clientId, DateTime.Parse(receivedAt), "CONSUMO", amount, 36);
}