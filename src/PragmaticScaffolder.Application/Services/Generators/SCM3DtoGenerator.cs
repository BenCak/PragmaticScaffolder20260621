using PragmaticScaffolder.Domain.Models;
using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Application.Services.Generators;

/// <summary>Generates SCM3-compliant DTOs, request models, and validators.</summary>
public sealed class SCM3DtoGenerator(ITemplateRenderer renderer) : ICodeGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationRequest request)
    {
        if (!request.IsSCM3Target)
            yield break;

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
                    c.MaxLength
                }).ToList();

            var createColumns = dtoColumns.Where(c => !c.IsIdentity).ToList();
            var updateColumns = dtoColumns.Where(c => !c.IsPrimaryKey && !c.IsIdentity).ToList();

            var model = new
            {
                Namespace     = $"{request.RootNamespace}.Shared",
                ClassName     = className,
                FeatureFolder = featureFolder,
                AllColumns    = dtoColumns,
                CreateColumns = createColumns,
                UpdateColumns = updateColumns
            };

            // Generate DTOs
            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Shared/{featureFolder}/{className}Dto.cs",
                Content      = renderer.Render("SCM3Dto", model)
            };

            // Generate validators
            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Shared/{featureFolder}/{className}Validators.cs",
                Content      = renderer.Render("SCM3Validators", model)
            };
        }
    }
}
