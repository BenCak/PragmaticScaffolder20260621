using PragmaticScaffolder.Domain.Models;
using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Application.Services.Generators;

/// <summary>Generates EF Core entity classes into the Data project.</summary>
public sealed class EntityGenerator(ITemplateRenderer renderer) : ICodeGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationRequest request)
    {
        foreach (var table in request.AllTables)
        {
            var className = NamingHelper.ToClassName(table.Name, request.TablePrefix);
            var pkColumns = table.PrimaryKeyColumns.ToList();
            var model = new
            {
                Namespace      = $"{request.RootNamespace}.Data",
                ClassName      = className,
                Table          = table,
                HasSinglePk    = pkColumns.Count == 1,
                HasCompositePk = pkColumns.Count > 1,
                PkColumns      = pkColumns.Select(c => new
                {
                    c.Name,
                    PropertyName = NamingHelper.ToPropertyName(c.Name)
                }).ToList(),
                Columns        = table.Columns.Select(c => new
                {
                    c.Name,
                    PropertyName = NamingHelper.ToPropertyName(c.Name),
                    c.ClrType,
                    c.IsNullable,
                    c.IsIdentity,
                    c.IsPrimaryKey,
                    c.IsComputed,
                    c.MaxLength,
                    c.DataType
                }).ToList()
            };

            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Data/Entities/{className}.cs",
                Content      = renderer.Render("Entity", model)
            };
        }
    }
}
