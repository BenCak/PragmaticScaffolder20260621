namespace PragmaticScaffolder.Domain.Models;

public sealed class SchemaMetadata
{
    public string Name { get; set; } = string.Empty;
    public List<TableMetadata>           Tables           { get; set; } = [];
    public List<TableMetadata>           Views            { get; set; } = [];
    public List<StoredProcedureMetadata> StoredProcedures { get; set; } = [];
}
