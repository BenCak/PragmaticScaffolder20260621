namespace PragmaticScaffolder.Domain.Models;

public sealed class ProcResultColumnMetadata
{
    public string Name      { get; set; } = string.Empty;
    public string DataType  { get; set; } = string.Empty;
    public string ClrType   { get; set; } = string.Empty;
    public bool   IsNullable { get; set; }
}
