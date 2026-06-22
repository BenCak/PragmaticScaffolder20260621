using PragmaticScaffolder.Domain.Models;
using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Application.Services.Generators;

/// <summary>Generates SCM3-compliant Blazor admin pages with role-based permission checks.</summary>
public sealed class SCM3BlazorPageGenerator(ITemplateRenderer renderer) : ICodeGenerator
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

            var gridColumns = table.Columns
                .Where(c => !c.IsComputed)
                .Where(c => c.DataType is not ("varbinary" or "image" or "timestamp" or "rowversion"))
                .Where(c => !c.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase))
                .Select(c => new
                {
                    c.Name,
                    PropertyName = NamingHelper.ToPropertyName(c.Name),
                    c.ClrType,
                    Label        = string.Join(" ", System.Text.RegularExpressions.Regex.Matches(c.Name, "[A-Z][a-z]*").Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value))
                })
                .ToList();

            var createFormColumns = table.Columns
                .Where(c => !c.IsComputed && !c.IsIdentity)
                .Where(c => c.DataType is not ("varbinary" or "image" or "timestamp" or "rowversion"))
                .Where(c => !c.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase))
                .Select(c => new
                {
                    c.Name,
                    PropertyName = NamingHelper.ToPropertyName(c.Name),
                    c.ClrType,
                    IsNullable   = c.IsNullable,
                    MaxLength   = c.MaxLength,
                    Label        = string.Join(" ", System.Text.RegularExpressions.Regex.Matches(c.Name, "[A-Z][a-z]*").Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value)),
                    IsDateType   = c.DataType is "datetime2" or "datetime" or "date"
                })
                .ToList();

            var model = new
            {
                Namespace        = $"{request.RootNamespace}.Web.Components.Pages.Admin",
                SharedNamespace  = $"{request.RootNamespace}.Shared",
                ClassName        = className,
                FeatureFolder    = featureFolder,
                RoutePrefix      = featureFolder.ToLowerInvariant(),
                HasSinglePk      = pkColumns.Count == 1,
                PkColumns        = pkColumns.Select(c => new
                {
                    c.Name,
                    PropertyName = NamingHelper.ToPropertyName(c.Name),
                    c.ClrType
                }).ToList(),
                GridColumns      = gridColumns,
                CreateFormColumns = createFormColumns
            };

            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Web/Components/Pages/Admin/{className}s.razor",
                Content      = renderer.Render("SCM3BlazorPage", model)
            };
        }
    }
}
