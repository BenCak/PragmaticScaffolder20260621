using PragmaticScaffolder.Domain.Models;
using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Application.Services.Generators;

/// <summary>
/// Generates NUnit service tests (Api.Tests) and bUnit component tests (Blazor.Tests).
/// </summary>
public sealed class TestsGenerator(ITemplateRenderer renderer) : ICodeGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationRequest request)
    {
        var ns = request.RootNamespace;

        foreach (var table in request.Tables)
        {
            var className     = NamingHelper.ToClassName(table.Name, request.TablePrefix);
            var featureFolder = NamingHelper.ToCollectionName(table.Name, request.TablePrefix);
            var pkColumns     = table.PrimaryKeyColumns.ToList();

            // Columns that go into CreateRequest (non-identity)
            var createColumns = table.Columns
                .Where(c => !c.IsComputed && !c.IsIdentity)
                .Where(c => c.DataType is not ("varbinary" or "image" or "timestamp" or "rowversion"))
                .Where(c => !c.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase))
                .Select(c => new
                {
                    c.Name,
                    PropertyName = NamingHelper.ToPropertyName(c.Name),
                    c.ClrType,
                    c.IsNullable,
                    SampleValue  = GetSampleValue(c)
                }).ToList();

            var model = new
            {
                Namespace       = ns,
                DataNamespace   = $"{ns}.Data",
                SharedNamespace = $"{ns}.Shared",
                ApiNamespace    = $"{ns}.Api.Features.{featureFolder}",
                BlazorNamespace = $"{ns}.Blazor.Features.{featureFolder}",
                ClassName       = className,
                FeatureFolder   = featureFolder,
                RoutePrefix     = featureFolder.ToLowerInvariant(),
                HasSinglePk     = pkColumns.Count == 1,
                PkColumns       = pkColumns.Select(c => new
                {
                    c.Name,
                    PropertyName = NamingHelper.ToPropertyName(c.Name),
                    c.ClrType,
                    ParamName    = NamingHelper.ToParamName(NamingHelper.ToPropertyName(c.Name)),
                    SampleValue  = GetSampleValue(c)
                }).ToList(),
                CreateColumns   = createColumns
            };

            if (request.GenerateApiTests)
                yield return new GeneratedFile
                {
                    RelativePath = $"tests/{ns}.Api.Tests/{featureFolder}/{featureFolder}ServiceTests.cs",
                    Content      = renderer.Render("ApiServiceTests", model)
                };

            if (request.GenerateBlazorTests)
                yield return new GeneratedFile
                {
                    RelativePath = $"tests/{ns}.Blazor.Tests/{featureFolder}/{featureFolder}PageTests.cs",
                    Content      = renderer.Render("BlazorPageTests", model)
                };
        }
    }

    private static string GetSampleValue(ColumnMetadata col)
    {
        var clr = col.ClrType.TrimEnd('?');
        return clr switch
        {
            "string"        => $"\"Test{NamingHelper.ToPropertyName(col.Name)}\"",
            "int"           => "42",
            "long"          => "42L",
            "short"         => "(short)1",
            "byte"          => "(byte)1",
            "bool"          => "false",
            "decimal"       => "9.99m",
            "double"        => "9.99",
            "float"         => "9.99f",
            "DateTime"      => "DateTime.UtcNow",
            "DateOnly"      => "DateOnly.FromDateTime(DateTime.Today)",
            "TimeOnly"      => "TimeOnly.MinValue",
            "DateTimeOffset"=> "DateTimeOffset.UtcNow",
            "Guid"          => "Guid.NewGuid()",
            _               => "default"
        };
    }
}
