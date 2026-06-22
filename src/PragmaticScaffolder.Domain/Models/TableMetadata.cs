namespace PragmaticScaffolder.Domain.Models;

public sealed class TableMetadata
{
    public string Schema { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string QualifiedName => $"[{Schema}].[{Name}]";
    public bool IsView { get; set; }
    public List<ColumnMetadata> Columns { get; set; } = [];
    public List<ForeignKeyMetadata> ForeignKeys { get; set; } = [];
    public IEnumerable<ColumnMetadata> PrimaryKeyColumns =>
        Columns.Where(c => c.IsPrimaryKey).OrderBy(c => c.OrdinalPosition);
}
