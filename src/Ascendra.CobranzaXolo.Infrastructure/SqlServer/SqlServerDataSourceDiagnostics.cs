using Ascendra.CobranzaXolo.Application.Diagnostics;

using Microsoft.Data.SqlClient;

namespace Ascendra.CobranzaXolo.Infrastructure.SqlServer;

public sealed class SqlServerDataSourceDiagnostics(
    SqlServerConnectionFactory connectionFactory) : IDataSourceDiagnostics
{
    private const string SummaryQuery = """
        SELECT
            (SELECT COUNT_BIG(*) FROM [Clientes].[Promesas]) AS Promesas,
            (SELECT COUNT_BIG(*) FROM [dbo].[pagos]) AS Pagos,
            (SELECT COUNT_BIG(*) FROM [dbo].[cartera]) AS RegistrosCartera,
            (
                SELECT COUNT_BIG(DISTINCT [CLIENTE_UNICO])
                FROM [dbo].[cartera]
                WHERE [FECHA_CARGA] = (SELECT MAX([FECHA_CARGA]) FROM [dbo].[cartera])
            ) AS ClientesCarteraActual,
            (SELECT MAX([FECHA_CARGA]) FROM [dbo].[cartera]) AS CarteraActualizadaEn;
        """;

    public async Task<DataSourceSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        if (!connectionFactory.IsConfigured)
        {
            return new DataSourceSnapshot(
                IsConfigured: false,
                IsReachable: false,
                Promesas: null,
                Pagos: null,
                RegistrosCartera: null,
                ClientesCarteraActual: null,
                CarteraActualizadaEn: null,
                Status: "SQL Server no está configurado.");
        }

        try
        {
            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(SummaryQuery, connection)
            {
                CommandTimeout = 15
            };

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);

            return new DataSourceSnapshot(
                IsConfigured: true,
                IsReachable: true,
                Promesas: reader.GetInt64(reader.GetOrdinal("Promesas")),
                Pagos: reader.GetInt64(reader.GetOrdinal("Pagos")),
                RegistrosCartera: reader.GetInt64(reader.GetOrdinal("RegistrosCartera")),
                ClientesCarteraActual: reader.IsDBNull(reader.GetOrdinal("ClientesCarteraActual"))
                    ? null
                    : reader.GetInt64(reader.GetOrdinal("ClientesCarteraActual")),
                CarteraActualizadaEn: reader.IsDBNull(reader.GetOrdinal("CarteraActualizadaEn"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("CarteraActualizadaEn")),
                Status: "SQL Server disponible.");
        }
        catch (SqlException)
        {
            return Unavailable();
        }
        catch (InvalidOperationException)
        {
            return Unavailable();
        }
    }

    private static DataSourceSnapshot Unavailable() => new(
        IsConfigured: true,
        IsReachable: false,
        Promesas: null,
        Pagos: null,
        RegistrosCartera: null,
        ClientesCarteraActual: null,
        CarteraActualizadaEn: null,
        Status: "No fue posible consultar SQL Server.");
}