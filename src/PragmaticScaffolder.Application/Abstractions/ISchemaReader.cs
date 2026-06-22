using PragmaticScaffolder.Domain.Models;

namespace PragmaticScaffolder.Application.Abstractions;

/// <summary>Reads database schema metadata from a target database.</summary>
public interface ISchemaReader
{
    Task<bool> TestConnectionAsync(string connectionString, CancellationToken ct = default);

    Task<DatabaseMetadata> ReadDatabaseAsync(string connectionString, CancellationToken ct = default);
}
