using PragmaticScaffolder.Domain.Models;
using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Application.Services.Generators;

/// <summary>Generates DTO and Request records in the Shared project.</summary>
public sealed class DtoGenerator(ITemplateRenderer renderer) : ICodeGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationRequest request)
    {
        var allTableLookup = request.AllTables
            .ToDictionary(t => $"{t.Schema}.{t.Name}", StringComparer.OrdinalIgnoreCase);

        foreach (var table in request.Tables)
        {
            var className     = NamingHelper.ToClassName(table.Name, request.TablePrefix);
            var featureFolder = NamingHelper.ToCollectionName(table.Name, request.TablePrefix);

            var dtoColumns = table.Columns
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
                    c.DataType
                }).ToList();

            var fkDisplays = BuildFkDisplays(table, allTableLookup, request.TablePrefix);

            var model = new
            {
                Namespace     = $"{request.RootNamespace}.Shared",
                ClassName     = className,
                FeatureFolder = featureFolder,
                AllColumns    = dtoColumns,
                // Request types exclude identity columns (user supplies non-identity PKs)
                CreateColumns = dtoColumns.Where(c => !c.IsIdentity).ToList(),
                UpdateColumns = dtoColumns.Where(c => !c.IsIdentity).ToList(),
                HasFkDisplay  = fkDisplays.Count > 0,
                FkDisplays    = fkDisplays
            };

            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Shared/{featureFolder}/{className}Dto.cs",
                Content      = renderer.Render("Dto", model)
            };

            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Shared/{featureFolder}/Create{className}Request.cs",
                Content      = renderer.Render("CreateRequest", model)
            };

            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Shared/{featureFolder}/Update{className}Request.cs",
                Content      = renderer.Render("UpdateRequest", model)
            };

            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Shared/{featureFolder}/Create{className}RequestValidator.cs",
                Content      = renderer.Render("CreateRequestValidator", model)
            };

            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Shared/{featureFolder}/Update{className}RequestValidator.cs",
                Content      = renderer.Render("UpdateRequestValidator", model)
            };
        }
    }

    internal static List<FkDisplay> BuildFkDisplays(
        TableMetadata table,
        Dictionary<string, TableMetadata> allTableLookup,
        string prefix = "")
    {
        var result = new List<FkDisplay>();
        var joinIndex = 0;
        foreach (var fk in table.ForeignKeys)
        {
            var key = $"{fk.ReferencedSchema}.{fk.ReferencedTable}";
            if (!allTableLookup.TryGetValue(key, out var refTable)) continue;

            var displayCol = NamingHelper.FindDisplayColumn(refTable, prefix);
            if (displayCol is null) continue;

            joinIndex++;
            var refClassName = NamingHelper.ToClassName(refTable.Name, prefix);
            var aliasBase = $"_{refClassName.ToLowerInvariant()[..Math.Min(3, refClassName.Length)]}";
            var aliasSuffix = joinIndex == 1 ? string.Empty : joinIndex.ToString();
            var fkNullable   = table.Columns
                .FirstOrDefault(c => c.Name.Equals(fk.ColumnName, StringComparison.OrdinalIgnoreCase))
                ?.IsNullable ?? true;

            result.Add(new FkDisplay
            {
                FkColumnName        = fk.ColumnName,
                FkPropertyName      = NamingHelper.ToPropertyName(fk.ColumnName),
                FkIsNullable        = fkNullable,
                RefClassName        = refClassName,
                RefSetName          = NamingHelper.ToCollectionName(refTable.Name, prefix),
                RefPkPropertyName   = NamingHelper.ToPropertyName(fk.ReferencedColumn),
                DisplayColumnName   = displayCol.Name,
                DisplayPropertyName = NamingHelper.ToPropertyName(displayCol.Name),
                DtoExPropertyName   = NamingHelper.ToDtoExPropertyName(fk.ColumnName, displayCol.Name),
                Alias               = $"{aliasBase}{aliasSuffix}",
                GroupAlias          = $"{aliasBase}j{aliasSuffix}"
            });
        }
        return result;
    }
}

public sealed class FkDisplay
{
    public string FkColumnName        { get; set; } = string.Empty;
    public string FkPropertyName      { get; set; } = string.Empty;
    public bool   FkIsNullable        { get; set; }
    public string RefClassName        { get; set; } = string.Empty;
    public string RefSetName          { get; set; } = string.Empty;
    public string RefPkPropertyName   { get; set; } = string.Empty;
    public string DisplayColumnName   { get; set; } = string.Empty;
    public string DisplayPropertyName { get; set; } = string.Empty;
    public string DtoExPropertyName   { get; set; } = string.Empty;
    public string Alias               { get; set; } = string.Empty;
    public string GroupAlias          { get; set; } = string.Empty;
}
