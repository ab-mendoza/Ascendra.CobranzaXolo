using Microsoft.Data.SqlClient;

namespace Ascendra.CobranzaXolo.Infrastructure.SqlServer;

public sealed class SqlServerConnectionFactory
{
    private const string Placeholder = "SET_IN_IIS_ENVIRONMENT";
    private readonly string? _connectionString;

    public SqlServerConnectionFactory(string? connectionString)
    {
        _connectionString = connectionString;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_connectionString) &&
        !_connectionString.Contains(Placeholder, StringComparison.OrdinalIgnoreCase);

    public SqlConnection CreateConnection()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "La cadena de conexión de SQL Server no está configurada.");
        }

        return new SqlConnection(_connectionString);
    }
}