namespace PragmaticScaffolder.Domain.Models;

public sealed class ColumnMetadata
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsPrimaryKey { get; set; }
    public int OrdinalPosition { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsComputed { get; set; }
    public string ClrType { get; set; } = string.Empty;
    public bool IsNullableClrType { get; set; }
}
