namespace PragmaticScaffolder.Domain.Models;

public sealed class StoredProcedureMetadata
{
    public string Schema        { get; set; } = string.Empty;
    public string Name          { get; set; } = string.Empty;
    public string QualifiedName => $"[{Schema}].[{Name}]";

    public List<ProcParameterMetadata>     Parameters     { get; set; } = [];
    public List<ProcResultColumnMetadata>  ResultColumns  { get; set; } = [];

    public bool    HasDescribableResult { get; set; }
    public string? DescribeError        { get; set; }

    public IEnumerable<ProcParameterMetadata> InputParameters
        => Parameters.Where(p => !p.IsOutput);
    public IEnumerable<ProcParameterMetadata> OutputParameters
        => Parameters.Where(p => p.IsOutput);
}
