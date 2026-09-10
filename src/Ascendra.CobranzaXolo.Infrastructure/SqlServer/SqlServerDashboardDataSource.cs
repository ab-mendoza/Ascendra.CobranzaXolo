using Ascendra.CobranzaXolo.Application.Dashboard;
using Ascendra.CobranzaXolo.Domain.Collections;

using Microsoft.Data.SqlClient;

namespace Ascendra.CobranzaXolo.Infrastructure.SqlServer;

/// <summary>
/// SQL Server implementation for the current dashboard source tables.
/// Queries are explicit, read-only and ordered for deterministic attribution.
/// </summary>
public sealed class SqlServerDashboardDataSource(
    SqlServerConnectionFactory connectionFactory) : IDashboardDataSource
{
    private const string PromisesQuery = """
        SELECT
            CONVERT(varchar(100), [IdPromesa]) AS IdPromesa,
            [Cliente_Unico],
            [Agente],
            [Fecha_Promesa],
            [Monto_Inicial],
            [Monto_Semanal],
            [Tipo_Promesa],
            [Numero_Semanas],
            [Fecha_Creacion]
        FROM [Clientes].[Promesas]
        ORDER BY [Fecha_Creacion], [IdPromesa];
        """;

    private const string PaymentsQuery = """
        SELECT
            [ID],
            [CLIENTE_UNICO],
            TRY_CONVERT(datetime2(0), LTRIM(RTRIM([FECHA_RECEPCION])), 103) AS FechaRecepcion,
            [PRODUCTO],
            TRY_CONVERT(decimal(18, 2), [RECUPERACION_GESTION]) AS RecuperacionPorGestion,
            TRY_CONVERT(int, [SEMANA]) AS Semana
        FROM [dbo].[pagos]
        ORDER BY [ID];
        """;

    private const string CurrentPortfolioQuery = """
        WITH CarteraActual AS
        (
            SELECT [CLIENTE_UNICO], [ESTATUS_PLAN]
            FROM [dbo].[cartera]
            WHERE [FECHA_CARGA] = (SELECT MAX([FECHA_CARGA]) FROM [dbo].[cartera])
        )
        SELECT [CLIENTE_UNICO], [ESTATUS_PLAN]
        FROM CarteraActual
        WHERE NULLIF(LTRIM(RTRIM([CLIENTE_UNICO])), '') IS NOT NULL;
        """;

    public async Task<IReadOnlyList<SourcePromise>> GetPromisesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(PromisesQuery, connection) { CommandTimeout = 30 };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var records = new List<SourcePromise>();
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new SourcePromise(
                ReadRequiredString(reader, "IdPromesa"),
                ReadString(reader, "Cliente_Unico"),
                ReadString(reader, "Agente"),
                ReadDateTime(reader, "Fecha_Promesa"),
                ReadDecimal(reader, "Monto_Inicial"),
                ReadDecimal(reader, "Monto_Semanal"),
                ReadString(reader, "Tipo_Promesa"),
                ReadInt32(reader, "Numero_Semanas"),
                ReadDateTime(reader, "Fecha_Creacion")));
        }

        return records;
    }

    public async Task<IReadOnlyList<SourcePayment>> GetPaymentsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(PaymentsQuery, connection) { CommandTimeout = 30 };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var records = new List<SourcePayment>();
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new SourcePayment(
                Convert.ToInt64(reader.GetValue(reader.GetOrdinal("ID"))),
                ReadString(reader, "CLIENTE_UNICO"),
                ReadDateTime(reader, "FechaRecepcion"),
                ReadString(reader, "PRODUCTO"),
                ReadDecimal(reader, "RecuperacionPorGestion") ?? 0m,
                ReadInt32(reader, "Semana")));
        }

        return records;
    }

    public async Task<IReadOnlyList<PortfolioAccount>> GetCurrentPortfolioAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(CurrentPortfolioQuery, connection) { CommandTimeout = 30 };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var records = new List<PortfolioAccount>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var clientId = ReadRequiredString(reader, "CLIENTE_UNICO");
            records.Add(new PortfolioAccount(clientId, ReadString(reader, "ESTATUS_PLAN")));
        }

        return records;
    }

    private static string ReadRequiredString(SqlDataReader reader, string column) =>
        ReadString(reader, column) ?? throw new InvalidOperationException(
            $"La columna requerida {column} contiene un valor nulo.");

    private static string? ReadString(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal))?.Trim();
    }

    private static DateTime? ReadDateTime(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToDateTime(reader.GetValue(ordinal));
    }

    private static decimal? ReadDecimal(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static int? ReadInt32(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    }
}