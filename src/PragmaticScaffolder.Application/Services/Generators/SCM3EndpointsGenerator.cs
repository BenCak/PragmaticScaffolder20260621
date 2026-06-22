using PragmaticScaffolder.Domain.Models;
using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Application.Services.Generators;

/// <summary>Generates SCM3-compliant minimal API endpoints with Result&lt;T&gt; and authorization.</summary>
public sealed class SCM3EndpointsGenerator(ITemplateRenderer renderer) : ICodeGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationRequest request)
    {
        if (!request.IsSCM3Target)
            yield break;

        foreach (var table in request.Tables)
        {
            var className     = NamingHelper.ToClassName(table.Name, request.TablePrefix);
            var featureFolder = NamingHelper.ToCollectionName(table.Name, request.TablePrefix);
            var pkColumns     = table.PrimaryKeyColumns.ToList();

            var model = new
            {
                Namespace       = $"{request.RootNamespace}.Api.Endpoints",
                SharedNamespace = $"{request.RootNamespace}.Shared",
                ClassName       = className,
                FeatureFolder   = featureFolder,
                RoutePrefix     = featureFolder.ToLowerInvariant(),
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
                RelativePath = $"src/{request.RootNamespace}.Api/Endpoints/{className}Endpoints.cs",
                Content      = renderer.Render("SCM3Endpoints", model)
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
