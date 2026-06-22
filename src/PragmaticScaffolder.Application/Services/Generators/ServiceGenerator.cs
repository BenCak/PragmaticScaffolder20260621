using PragmaticScaffolder.Domain.Models;
using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Application.Services.Generators;

/// <summary>Generates feature service classes in the Api project.</summary>
public sealed class ServiceGenerator(ITemplateRenderer renderer) : ICodeGenerator
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
            var hasSoftDelete = table.Columns.Any(c =>
                c.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase) && c.DataType == "bit");

            var allDtoColumns = table.Columns
                .Where(c => !c.IsComputed)
                .Where(c => c.DataType is not ("varbinary" or "image" or "timestamp" or "rowversion"))
                .Where(c => !c.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase))
                .Select(c => new
                {
                    c.Name,
                    PropertyName = NamingHelper.ToPropertyName(c.Name),
                    c.ClrType,
                    c.IsNullable
                })
                .ToList();

            var searchColumns = allDtoColumns
                .Where(c => c.ClrType is "string" or "string?")
                .Select(c => new { c.PropertyName, c.IsNullable })
                .ToList();

            var createSetColumns = table.Columns
                .Where(c => !c.IsComputed && !c.IsIdentity)
                .Where(c => c.DataType is not ("varbinary" or "image" or "timestamp" or "rowversion"))
                .Where(c => !c.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase))
                .Select(c => new { c.Name, PropertyName = NamingHelper.ToPropertyName(c.Name) })
                .ToList();

            var updateSetColumns = table.Columns
                .Where(c => !c.IsPrimaryKey && !c.IsComputed && !c.IsIdentity)
                .Where(c => c.DataType is not ("varbinary" or "image" or "timestamp" or "rowversion"))
                .Where(c => !c.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase))
                .Select(c => new { c.Name, PropertyName = NamingHelper.ToPropertyName(c.Name) })
                .ToList();

            var fkDisplays = DtoGenerator.BuildFkDisplays(table, allTableLookup, request.TablePrefix);

            var model = new
            {
                Namespace        = $"{request.RootNamespace}.Api.Features.{featureFolder}",
                DataNamespace    = $"{request.RootNamespace}.Data",
                SharedNamespace  = $"{request.RootNamespace}.Shared",
                ClassName        = className,
                FeatureFolder    = featureFolder,
                SetName          = NamingHelper.ToCollectionName(table.Name, request.TablePrefix),
                HasSoftDelete    = hasSoftDelete,
                HasSinglePk      = pkColumns.Count == 1,
                PkColumns        = pkColumns.Select(c => new
                {
                    c.Name,
                    PropertyName = NamingHelper.ToPropertyName(c.Name),
                    c.ClrType,
                    ParamName    = NamingHelper.ToParamName(NamingHelper.ToPropertyName(c.Name))
                }).ToList(),
                AllDtoColumns    = allDtoColumns,
                CreateSetColumns = createSetColumns,
                UpdateSetColumns = updateSetColumns,
                HasFkDisplay     = fkDisplays.Count > 0,
                FkDisplays       = fkDisplays,
                SearchColumns    = searchColumns
            };

            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Api/Features/{featureFolder}/{className}Service.cs",
                Content      = renderer.Render("Service", model)
            };
        }
    }
}
