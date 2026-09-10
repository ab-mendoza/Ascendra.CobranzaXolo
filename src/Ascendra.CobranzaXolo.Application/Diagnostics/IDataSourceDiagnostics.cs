namespace Ascendra.CobranzaXolo.Application.Diagnostics;

public interface IDataSourceDiagnostics
{
    Task<DataSourceSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}