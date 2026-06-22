using PragmaticScaffolder.Domain.Models;
using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Application.Services.Generators;

/// <summary>Generates AppDbContext in the Data project.</summary>
public sealed class DbContextGenerator(ITemplateRenderer renderer) : ICodeGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationRequest request)
    {
        var model = new
        {
            Namespace  = $"{request.RootNamespace}.Data",
            Entities   = request.AllTables.Select(t => new
            {
                ClassName = NamingHelper.ToClassName(t.Name, request.TablePrefix),
                SetName   = NamingHelper.ToCollectionName(t.Name, request.TablePrefix),
                t.Schema,
                t.Name
            }).ToList()
        };

        yield return new GeneratedFile
        {
            RelativePath = $"src/{request.RootNamespace}.Data/AppDbContext.cs",
            Content      = renderer.Render("DbContext", model)
        };
    }
}
