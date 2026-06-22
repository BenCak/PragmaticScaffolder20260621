using PragmaticScaffolder.Domain.Models;
using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Application.Services.Generators;

/// <summary>Generates SCM3-compliant services with Result&lt;T&gt;, audit, and permission checks.</summary>
public sealed class SCM3ServiceGenerator(ITemplateRenderer renderer) : ICodeGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationRequest request)
    {
        if (!request.IsSCM3Target)
            yield break;

        var allTableLookup = request.AllTables
            .ToDictionary(t => $"{t.Schema}.{t.Name}", StringComparer.OrdinalIgnoreCase);

        foreach (var table in request.Tables)
        {
            var className     = NamingHelper.ToClassName(table.Name, request.TablePrefix);
            var featureFolder = NamingHelper.ToCollectionName(table.Name, request.TablePrefix);
            var pkColumns     = table.PrimaryKeyColumns.ToList();

            var createSetColumns = table.Columns
                .Where(c => !c.IsComputed && !c.IsIdentity)
                .Where(c => c.DataType is not ("varbinary" or "image" or "timestamp" or "rowversion"))
                .Where(c => !c.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase))
                .Select(c => new { c.Name, PropertyName = NamingHelper.ToPropertyName(c.Name) })
                .ToList();

            var model = new
            {
                Namespace        = $"{request.RootNamespace}.Services",
                DataNamespace    = $"{request.RootNamespace}.Data",
                SharedNamespace  = $"{request.RootNamespace}.Shared",
                ClassName        = className,
                FeatureFolder    = featureFolder,
                SetName          = NamingHelper.ToCollectionName(table.Name, request.TablePrefix),
                HasSoftDelete    = table.Columns.Any(c =>
                    c.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase) && c.DataType == "bit"),
                HasSinglePk      = pkColumns.Count == 1,
                PkColumns        = pkColumns.Select(c => new
                {
                    c.Name,
                    PropertyName = NamingHelper.ToPropertyName(c.Name),
                    c.ClrType,
                    ParamName    = NamingHelper.ToParamName(NamingHelper.ToPropertyName(c.Name))
                }).ToList(),
                CreateSetColumns = createSetColumns
            };

            // Generate service interface
            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Services/I{className}Service.cs",
                Content      = renderer.Render("SCM3ServiceInterface", model)
            };

            // Generate service implementation
            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Services/{className}Service.cs",
                Content      = renderer.Render("SCM3Service", model)
            };
        }
    }
}
