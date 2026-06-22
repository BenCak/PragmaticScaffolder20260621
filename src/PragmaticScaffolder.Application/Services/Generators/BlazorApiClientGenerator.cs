using PragmaticScaffolder.Domain.Models;
using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Application.Services.Generators;

/// <summary>
/// Generates a typed HttpClient wrapper per feature in the Blazor project.
/// Blazor pages call the API through these clients — never touching DbContext directly.
/// </summary>
public sealed class BlazorApiClientGenerator(ITemplateRenderer renderer) : ICodeGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationRequest request)
    {
        var allTableLookup = request.AllTables
            .ToDictionary(t => $"{t.Schema}.{t.Name}", StringComparer.OrdinalIgnoreCase);

        foreach (var table in request.Tables)
        {
            var className     = NamingHelper.ToClassName(table.Name, request.TablePrefix);
            var featureFolder = NamingHelper.ToCollectionName(table.Name, request.TablePrefix);
            var pkColumns     = table.PrimaryKeyColumns.ToList();
            var fkDisplays    = DtoGenerator.BuildFkDisplays(table, allTableLookup, request.TablePrefix);

            var model = new
            {
                Namespace       = $"{request.RootNamespace}.Blazor.Features.{featureFolder}",
                SharedNamespace = $"{request.RootNamespace}.Shared",
                ClassName       = className,
                FeatureFolder   = featureFolder,
                RoutePrefix     = featureFolder.ToLowerInvariant(),
                HasFkDisplay    = fkDisplays.Count > 0,
                HasSinglePk     = pkColumns.Count == 1,
                PkColumns       = pkColumns.Select(c => new
                {
                    c.Name,
                    PropertyName    = NamingHelper.ToPropertyName(c.Name),
                    c.ClrType,
                    ParamName       = NamingHelper.ToParamName(NamingHelper.ToPropertyName(c.Name)),
                    RouteConstraint = GetRouteConstraint(c.ClrType)
                }).ToList()
            };

            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Blazor/Features/{featureFolder}/{featureFolder}ApiClient.cs",
                Content      = renderer.Render("BlazorApiClient", model)
            };
        }
    }

    private static string GetRouteConstraint(string clrType) => clrType.TrimEnd('?') switch
    {
        "int"  => ":int",
        "long" => ":long",
        "Guid" => ":guid",
        _      => string.Empty
    };
}
