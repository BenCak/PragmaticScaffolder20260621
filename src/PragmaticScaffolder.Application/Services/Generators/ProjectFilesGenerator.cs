using PragmaticScaffolder.Domain.Models;
using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Application.Services.Generators;

/// <summary>
/// Generates solution-level files for the generated Blazor Server app.
/// </summary>
public sealed class ProjectFilesGenerator(ITemplateRenderer renderer) : ICodeGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationRequest request)
    {
        var ns = request.RootNamespace;
        var features = request.Tables.Select(t => new
        {
            ClassName     = NamingHelper.ToClassName(t.Name, request.TablePrefix),
            FeatureFolder = NamingHelper.ToCollectionName(t.Name, request.TablePrefix),
            RoutePrefix   = NamingHelper.ToCollectionName(t.Name, request.TablePrefix).ToLowerInvariant()
        }).ToList();

        var procs = request.StoredProcedures.Select(sp =>
        {
            var (featureFolder, className, actionName) =
                NamingHelper.ParseProcName(sp.Name, request.SpPrefix, request.TablePrefix);
            var procClass = className + actionName;
            return new
            {
                FeatureFolder   = featureFolder,
                ProcClass       = procClass,
                ApiClientClass  = procClass + "ApiClient",
                ServiceClass    = procClass + "Service",
                Route           = $"{featureFolder.ToLowerInvariant()}/{actionName.ToLowerInvariant()}",
                PageRoute       = $"{featureFolder.ToLowerInvariant()}/{actionName.ToLowerInvariant()}",
                NavLabel        = NamingHelper.ToLabel(featureFolder) + " — " + NamingHelper.ToLabel(actionName)
            };
        }).ToList();

        yield return Render($"src/{ns}.Shared/PagedResult.cs",
            "PagedResult", new { Namespace = $"{ns}.Shared" });
        yield return Render($"src/{ns}.Shared/{ns}.Shared.csproj",
            "SharedProject", new { Namespace = ns });
        yield return Render($"src/{ns}.Data/{ns}.Data.csproj",
            "DataProject", new { Namespace = ns });
        yield return Render($"src/{ns}.Api/{ns}.Api.csproj",
            "ApiProject", new { Namespace = ns });
        yield return Render($"src/{ns}.Blazor/{ns}.Blazor.csproj",
            "BlazorProject", new { Namespace = ns });

        if (request.GenerateApiTests)
            yield return Render($"tests/{ns}.Api.Tests/{ns}.Api.Tests.csproj",
                "ApiTestProject", new { Namespace = ns });

        if (request.GenerateBlazorTests)
            yield return Render($"tests/{ns}.Blazor.Tests/{ns}.Blazor.Tests.csproj",
                "BlazorTestProject", new { Namespace = ns });

        yield return Render($"src/{ns}.Api/Program.cs",
            "ApiProgram", new { Namespace = ns, Features = features, Procs = procs });
        yield return Render($"src/{ns}.Api/appsettings.json",
            "AppSettings", new { Namespace = ns, ConnectionString = request.ConnectionString });
        yield return Render($"src/{ns}.Api/Properties/launchSettings.json",
            "ApiLaunchSettings", new { Namespace = ns });

        yield return Render($"src/{ns}.Blazor/Program.cs",
            "BlazorProgram", new { Namespace = ns, Features = features, Procs = procs });
        yield return Render($"src/{ns}.Blazor/appsettings.json",
            "BlazorAppSettings", new { Namespace = ns });
        yield return Render($"src/{ns}.Blazor/Properties/launchSettings.json",
            "BlazorLaunchSettings", new { Namespace = ns });
        yield return Render($"src/{ns}.Blazor/App.razor",
            "AppRazor", new { Namespace = ns });
        yield return Render($"src/{ns}.Blazor/Routes.razor",
            "Routes", new { Namespace = ns });
        yield return Render($"src/{ns}.Blazor/Pages/Home.razor",
            "HomePage", new { Namespace = ns, Features = features, Procs = procs });
        yield return Render($"src/{ns}.Blazor/_Imports.razor",
            "Imports", new { Namespace = ns });
        yield return Render($"src/{ns}.Blazor/wwwroot/app.css",
            "BlazorAppCss", new { Namespace = ns });
        yield return Render($"src/{ns}.Blazor/Components/Layout/MainLayout.razor",
            "MainLayout", new { Namespace = ns, Features = features, Procs = procs });

        yield return Render($"{ns}.sln",
            "Solution", new
            {
                Namespace           = ns,
                GenerateApiTests    = request.GenerateApiTests,
                GenerateBlazorTests = request.GenerateBlazorTests
            });
        yield return Render("docker/Dockerfile.Api",
            "DockerfileApi", new { Namespace = ns });
        yield return Render("docker/Dockerfile.Blazor",
            "DockerfileBlazor", new { Namespace = ns });
        yield return Render("docker/docker-compose.yml",
            "DockerCompose", new { Namespace = ns });
        yield return Render("README.md",
            "Readme", new { Namespace = ns, Features = features });
    }

    private GeneratedFile Render(string path, string template, object model) => new()
    {
        RelativePath = path,
        Content      = renderer.Render(template, model)
    };
}
