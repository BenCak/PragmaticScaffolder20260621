namespace PragmaticScaffolder.Application.Abstractions;

/// <summary>Generates one slice of the scaffolded output for a given request.</summary>
public interface ICodeGenerator
{
    IEnumerable<GeneratedFile> Generate(GenerationRequest request);
}
