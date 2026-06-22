// Generates the NWS app from the real Northwind SQL Server database into /home/ben/code/nws
// Run with: dotnet run --project src/PragmaticScaffolder.TestGen
using PragmaticScaffolder.Application;
using PragmaticScaffolder.Application.Abstractions;
using PragmaticScaffolder.Application.Services;
using PragmaticScaffolder.Application.Services.Generators;
using PragmaticScaffolder.Infrastructure.Database;
using PragmaticScaffolder.Infrastructure.FileSystem;
using PragmaticScaffolder.Infrastructure.Templates;

const string ConnStr =
    "Data Source=localhost;Initial Catalog=northwind;User ID=sa;Password=Sa123465!;" +
    "Pooling=False;Connect Timeout=30;Encrypt=False;" +
    "Trust Server Certificate=True;Authentication=SqlPassword;" +
    "Application Name=vscode-mssql;Application Intent=ReadWrite;Command Timeout=30";

var outputPath = Environment.GetEnvironmentVariable("SCAFFOLDER_TEST_OUTPUT") ?? "/home/ben/code/nws";

Console.WriteLine("Connecting to Northwind...");
var reader = new SqlServerSchemaReader();

var ok = await reader.TestConnectionAsync(ConnStr);
if (!ok) { Console.Error.WriteLine("Connection failed."); return 1; }

var db = await reader.ReadDatabaseAsync(ConnStr);
Console.WriteLine($"Connected: {db.DatabaseName} on {db.ServerVersion}");

var allTables = db.AllTables.ToList();
Console.WriteLine($"Found {allTables.Count} tables: {string.Join(", ", allTables.Select(t => t.Name))}");

if (Directory.Exists(outputPath))
    Directory.Delete(outputPath, recursive: true);
Directory.CreateDirectory(outputPath);

// Select the test SP (usp_tblOrders_Search) if it exists
var selectedProcs = db.AllStoredProcedures
    .Where(sp => sp.Name == "usp_tblOrders_Search" && sp.HasDescribableResult)
    .ToList();

if (selectedProcs.Count > 0)
    Console.WriteLine($"SP selected: {selectedProcs[0].Name} — {selectedProcs[0].InputParameters.Count()} params, {selectedProcs[0].ResultColumns.Count} cols");
else
    Console.WriteLine("No describable SPs found (skipping SP generation)");

var request = new GenerationRequest
{
    RootNamespace    = "nws",
    OutputPath       = outputPath,
    Tables           = allTables,
    AllTables        = allTables,
    ConnectionString = ConnStr,
    SpPrefix         = "usp_",
    TablePrefix      = "tbl",
    StoredProcedures = selectedProcs
};

Console.WriteLine("Generating...");

// Manual composition root — no DI container in a top-level-statement console app.
ITemplateRenderer renderer = new ScribanTemplateRenderer();
IGeneratedFileWriter fileWriter = new FileSystemGeneratedFileWriter();
ICodeGenerator[] generators =
[
    new EntityGenerator(renderer),
    new DbContextGenerator(renderer),
    new DtoGenerator(renderer),
    new ServiceGenerator(renderer),
    new EndpointsGenerator(renderer),
    new BlazorApiClientGenerator(renderer),
    new BlazorPageGenerator(renderer),
    new TestsGenerator(renderer),
    new ProjectFilesGenerator(renderer),
    new StoredProcedureGenerator(renderer),
];

var engine = new GenerationEngine(generators, fileWriter);
var result = engine.Generate(request);

if (result.Success)
{
    Console.WriteLine($"OK — {result.Files.Count} files written to {outputPath}");
}
else
{
    Console.WriteLine($"FAILED ({result.Errors.Count} errors):");
    foreach (var e in result.Errors)
        Console.WriteLine($"  ERROR: {e}");
    return 1;
}

return 0;
