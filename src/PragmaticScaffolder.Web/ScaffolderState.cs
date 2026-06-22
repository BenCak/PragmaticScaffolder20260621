using PragmaticScaffolder.Domain.Models;

namespace PragmaticScaffolder.Web;

/// <summary>
/// Holds wizard state across pages (connection → table selection → generate).
/// Scoped to the Blazor circuit, so one instance per browser session.
/// </summary>
public sealed class ScaffolderState
{
    public string ConnectionString { get; set; } = string.Empty;
    public DatabaseMetadata? Database { get; set; }
    public HashSet<string> SelectedTableKeys { get; set; } = [];  // "schema.table"
    public string RootNamespace { get; set; } = "MyApp";
    public string OutputPath { get; set; } = string.Empty;
    public string TablePrefix { get; set; } = string.Empty;
    public string SpPrefix    { get; set; } = "usp_";
    public bool GenerateApiTests { get; set; } = true;
    public bool GenerateBlazorTests { get; set; } = true;

    public HashSet<string> SelectedProcKeys { get; set; } = [];

    public List<StoredProcedureMetadata> SelectedStoredProcedures =>
        Database?.AllStoredProcedures
            .Where(sp => SelectedProcKeys.Contains($"{sp.Schema}.{sp.Name}"))
            .ToList() ?? [];

    public List<TableMetadata> SelectedTables =>
        Database?.AllTables
            .Where(t => SelectedTableKeys.Contains($"{t.Schema}.{t.Name}"))
            .ToList() ?? [];
}
