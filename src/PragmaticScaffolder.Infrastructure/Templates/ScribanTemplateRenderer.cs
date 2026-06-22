using System.Reflection;
using System.Text;
using Scriban;
using Scriban.Runtime;
using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Infrastructure.Templates;

/// <summary>
/// Renders Scriban templates loaded from embedded resources.
/// Template files live in Templates/Files/*.scriban and are embedded in the assembly.
/// </summary>
public sealed class ScribanTemplateRenderer : ITemplateRenderer
{
    private static readonly Assembly ThisAssembly = typeof(ScribanTemplateRenderer).Assembly;

    public string Render(string templateName, object model)
    {
        var source = LoadSource(templateName);
        var template = Template.Parse(source);

        if (template.HasErrors)
            throw new InvalidOperationException(
                $"Template '{templateName}' errors: {string.Join(", ", template.Messages)}");

        var context = new TemplateContext { StrictVariables = false };
        var scriptObj = new ScriptObject();
        scriptObj.Import(model, renamer: ToSnakeCase);
        context.PushGlobal(scriptObj);

        return template.Render(context);
    }

    private static string LoadSource(string templateName)
    {
        var resourceName = $"PragmaticScaffolder.Infrastructure.Templates.Files.{templateName}.scriban";
        using var stream = ThisAssembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException(
                $"Embedded template '{resourceName}' not found. Available: {string.Join(", ", ThisAssembly.GetManifestResourceNames())}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // Converts PascalCase member names to snake_case for Scriban templates
    private static string ToSnakeCase(MemberInfo member)
    {
        var name = member.Name;
        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0)
                sb.Append('_');
            sb.Append(char.ToLowerInvariant(name[i]));
        }
        return sb.ToString();
    }
}
