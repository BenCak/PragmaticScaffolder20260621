namespace PragmaticScaffolder.Domain.Models;

public sealed class ForeignKeyMetadata
{
    public string Name { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string ReferencedSchema { get; set; } = string.Empty;
    public string ReferencedTable { get; set; } = string.Empty;
    public string ReferencedColumn { get; set; } = string.Empty;
    public string DeleteAction { get; set; } = string.Empty;
    public string UpdateAction { get; set; } = string.Empty;
}
