namespace PragmaticScaffolder.Application.Abstractions;

/// <summary>Renders a named template against a model into source text.</summary>
public interface ITemplateRenderer
{
    string Render(string templateName, object model);
}
