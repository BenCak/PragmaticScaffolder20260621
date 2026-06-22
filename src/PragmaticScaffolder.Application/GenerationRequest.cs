using PragmaticScaffolder.Domain.Models;

namespace PragmaticScaffolder.Application;

public sealed class GenerationRequest
{
    /// <summary>Root namespace for the generated app (e.g. "MyApp").</summary>
    public string RootNamespace { get; set; } = string.Empty;

    /// <summary>Folder where the generated solution will be written.</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>Tables the user selected to scaffold.</summary>
    public List<TableMetadata> Tables { get; set; } = [];

    /// <summary>All tables in the database — used to resolve FK display names.</summary>
    public List<TableMetadata> AllTables { get; set; } = [];

    /// <summary>SQL Server connection string written into the generated appsettings.json.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Table name prefix to strip from generated class/route names (e.g. "tbl" turns "tblCustomer" → "Customer").</summary>
    public string TablePrefix { get; set; } = string.Empty;

    /// <summary>Stored procedure name prefix to strip (e.g. "usp_" turns "usp_tblOrders_Search" → "tblOrders_Search").</summary>
    public string SpPrefix { get; set; } = string.Empty;

    /// <summary>Stored procedures selected for scaffolding.</summary>
    public List<StoredProcedureMetadata> StoredProcedures { get; set; } = [];

    /// <summary>Whether to generate the Api.Tests project and test stubs.</summary>
    public bool GenerateApiTests { get; set; } = true;

    /// <summary>Whether to generate the Blazor.Tests project and test stubs.</summary>
    public bool GenerateBlazorTests { get; set; } = true;

    /// <summary>When true, generates SCM3-compliant code with Result&lt;T&gt;, audit logging, repositories, and permission checks.</summary>
    public bool IsSCM3Target { get; set; } = false;
}
