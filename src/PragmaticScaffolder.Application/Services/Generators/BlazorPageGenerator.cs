using PragmaticScaffolder.Domain.Models;
using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Application.Services.Generators;

public sealed class GridCol(string name, string propertyName, string clrType, bool isNullable)
{
    public string Name         { get; } = name;
    public string PropertyName { get; } = propertyName;
    public string ClrType      { get; } = clrType;
    public bool   IsNullable   { get; } = isNullable;
}

/// <summary>Generates Telerik UI for Blazor pages in the Blazor project. Pages call ApiClient — never DbContext.</summary>
public sealed class BlazorPageGenerator(ITemplateRenderer renderer) : ICodeGenerator
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
            var hasFkDisplay  = fkDisplays.Count > 0;

            var fkColumnNames = fkDisplays
                .Select(f => f.FkColumnName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var formColumns = table.Columns
                .Where(c => !c.IsComputed)
                .Where(c => c.DataType is not ("varbinary" or "image" or "timestamp" or "rowversion"))
                .Where(c => !c.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase))
                .Select(c => new
                {
                    c.Name,
                    PropertyName = NamingHelper.ToPropertyName(c.Name),
                    c.ClrType,
                    c.IsNullable,
                    c.IsIdentity,
                    c.IsPrimaryKey,
                    c.MaxLength,
                    c.DataType,
                    IsDateType = c.DataType is "date" or "datetime" or "datetime2" or "smalldatetime",
                    IsNumeric  = c.DataType is "int" or "bigint" or "smallint" or "tinyint"
                                    or "decimal" or "numeric" or "float" or "real" or "money" or "smallmoney",
                    IsBool     = c.DataType == "bit",
                    Label      = NamingHelper.ToLabel(c.Name)
                }).ToList();

            var gridColumns = new List<GridCol>();
            foreach (var c in formColumns
                .Where(c => !c.IsIdentity || c.IsPrimaryKey)
                .Where(c => !(c.IsPrimaryKey && c.IsNumeric))   // hide numeric auto-increment PKs from the grid
                .Where(c => !hasFkDisplay || !fkColumnNames.Contains(c.Name))
                .Take(hasFkDisplay ? 5 : 6))
            {
                gridColumns.Add(new GridCol(c.Name, c.PropertyName, c.ClrType, c.IsNullable));
            }
            foreach (var fk in fkDisplays)
                gridColumns.Add(new GridCol(fk.DtoExPropertyName, fk.DtoExPropertyName, "string?", true));

            var createFormColumns = formColumns.Where(c => !c.IsIdentity).ToList();
            var editFormColumns   = formColumns.Where(c => !c.IsIdentity).ToList();
            var stringFilterCols  = gridColumns
                .Where(c => c.ClrType is "string" or "string?")
                .Select(c => new { c.PropertyName, c.IsNullable })
                .ToList();

            var model = new
            {
                Namespace         = $"{request.RootNamespace}.Blazor.Features.{featureFolder}",
                SharedNamespace   = $"{request.RootNamespace}.Shared",
                ClassName         = className,
                FeatureFolder     = featureFolder,
                RoutePrefix       = featureFolder.ToLowerInvariant(),
                // Grid uses DtoEx when FKs are present, plain Dto otherwise
                GridDtoType       = hasFkDisplay ? $"{className}DtoEx" : $"{className}Dto",
                HasFkDisplay      = hasFkDisplay,
                HasSinglePk       = pkColumns.Count == 1,
                PkColumns         = pkColumns.Select(c => new
                {
                    c.Name,
                    PropertyName = NamingHelper.ToPropertyName(c.Name),
                    c.ClrType,
                    ParamName    = NamingHelper.ToParamName(NamingHelper.ToPropertyName(c.Name))
                }).ToList(),
                GridColumns         = gridColumns,
                CreateFormColumns   = createFormColumns,
                EditFormColumns     = editFormColumns,
                HasStringFilter     = stringFilterCols.Count > 0,
                StringFilterColumns = stringFilterCols
            };

            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Blazor/Features/{featureFolder}/{featureFolder}Page.razor",
                Content      = renderer.Render("BlazorPage", model)
            };
        }
    }
}
