using PragmaticScaffolder.Domain.Models;
using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Application.Services.Generators;

public sealed class StoredProcedureGenerator(ITemplateRenderer renderer) : ICodeGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationRequest request)
    {
        if (request.StoredProcedures.Count == 0) yield break;

        var ns = request.RootNamespace;

        foreach (var sp in request.StoredProcedures)
        {
            var (featureFolder, className, actionName) =
                NamingHelper.ParseProcName(sp.Name, request.SpPrefix, request.TablePrefix);

            var procClass  = className + actionName;
            var route      = $"{featureFolder.ToLowerInvariant()}/{actionName.ToLowerInvariant()}";
            var pageRoute  = route;
            var pageTitle  = NamingHelper.ToLabel(featureFolder) + " — " + NamingHelper.ToLabel(actionName);

            var inputParams = sp.InputParameters.Select(p => new
            {
                ParameterName = p.ParameterName,
                PropertyName  = NamingHelper.ToPropertyName(p.PropertyName),
                Label         = NamingHelper.ToLabel(p.PropertyName),
                ClrType       = p.ClrType,
                DataType      = p.DataType,
                IsDateType    = p.DataType is "date" or "datetime" or "datetime2" or "smalldatetime",
                IsNumeric     = p.DataType is "int" or "bigint" or "smallint" or "tinyint"
                                    or "decimal" or "numeric" or "float" or "real" or "money" or "smallmoney",
                IsBool        = p.DataType == "bit"
            }).ToList();

            var resultColumns = sp.ResultColumns.Select(c => new
            {
                c.Name,
                PropertyName = NamingHelper.ToPropertyName(c.Name),
                c.ClrType,
                BaseClrType  = c.ClrType.TrimEnd('?'),
                c.IsNullable,
                DefaultValue = GetDefaultValue(c.ClrType)
            }).ToList();

            var model = new
            {
                Namespace         = $"{ns}.Api.Features.{featureFolder}",
                BlazorNamespace   = $"{ns}.Blazor.Features.{featureFolder}",
                DataNamespace     = $"{ns}.Data",
                SharedNamespace   = $"{ns}.Shared",
                FeatureFolder     = featureFolder,
                ClassName         = className,
                ActionName        = actionName,
                ProcClass         = procClass,
                QualifiedProcName = sp.QualifiedName,
                Route             = route,
                PageRoute         = pageRoute,
                PageTitle         = pageTitle,
                HasRequest        = inputParams.Count > 0,
                InputParams       = inputParams,
                ResultColumns     = resultColumns
            };

            yield return Render(
                $"src/{ns}.Shared/{featureFolder}/{procClass}Result.cs",
                "ProcResult", model);

            if (inputParams.Count > 0)
                yield return Render(
                    $"src/{ns}.Shared/{featureFolder}/{procClass}Request.cs",
                    "ProcRequest", model);

            yield return Render(
                $"src/{ns}.Api/Features/{featureFolder}/{procClass}Service.cs",
                "ProcService", model);

            yield return Render(
                $"src/{ns}.Api/Features/{featureFolder}/{procClass}Endpoints.cs",
                "ProcEndpoints", model);

            yield return Render(
                $"src/{ns}.Blazor/Features/{featureFolder}/{procClass}ApiClient.cs",
                "ProcApiClient", model);

            yield return Render(
                $"src/{ns}.Blazor/Features/{featureFolder}/{procClass}Page.razor",
                "ProcPage", model);
        }
    }

    private static string GetDefaultValue(string clrType) => clrType.TrimEnd('?') switch
    {
        "string"          => "null",
        "int"             => "0",
        "long"            => "0L",
        "short"           => "(short)0",
        "byte"            => "(byte)0",
        "bool"            => "false",
        "decimal"         => "0m",
        "double"          => "0.0",
        "float"           => "0f",
        "DateTime"        => "DateTime.MinValue",
        "DateOnly"        => "DateOnly.MinValue",
        "DateTimeOffset"  => "DateTimeOffset.MinValue",
        "Guid"            => "Guid.Empty",
        _                 => "default"
    };

    private GeneratedFile Render(string path, string template, object model) => new()
    {
        RelativePath = path,
        Content      = renderer.Render(template, model)
    };
}
