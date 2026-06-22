namespace PragmaticScaffolder.Domain.Models;

public sealed class ProcParameterMetadata
{
    public string  ParameterName { get; set; } = string.Empty; // includes @
    public string  DataType      { get; set; } = string.Empty;
    public string  ClrType       { get; set; } = string.Empty;
    public bool    IsOutput      { get; set; }
    public int?    MaxLength     { get; set; }
    public int?    Precision     { get; set; }
    public int?    Scale         { get; set; }
    public int     OrdinalPosition { get; set; }

    public string PropertyName => ParameterName.TrimStart('@');
}
