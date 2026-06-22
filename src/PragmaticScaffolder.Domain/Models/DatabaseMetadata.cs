namespace PragmaticScaffolder.Domain.Models;

public sealed class DatabaseMetadata
{
    public string DatabaseName { get; set; } = string.Empty;
    public string ServerVersion { get; set; } = string.Empty;
    public List<SchemaMetadata> Schemas { get; set; } = [];

    public IEnumerable<TableMetadata> AllTables =>
        Schemas.SelectMany(s => s.Tables);

    public IEnumerable<StoredProcedureMetadata> AllStoredProcedures =>
        Schemas.SelectMany(s => s.StoredProcedures);
}
