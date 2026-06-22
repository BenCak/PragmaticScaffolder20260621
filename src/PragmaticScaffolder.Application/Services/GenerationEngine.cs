using PragmaticScaffolder.Application.Abstractions;

namespace PragmaticScaffolder.Application.Services;

/// <summary>
/// Orchestrates all registered <see cref="ICodeGenerator"/> implementations and, when generating
/// for real, hands the result to an <see cref="IGeneratedFileWriter"/>.
/// </summary>
public sealed class GenerationEngine(IEnumerable<ICodeGenerator> generators, IGeneratedFileWriter fileWriter)
{
    private readonly IReadOnlyList<ICodeGenerator> _generators = [.. generators];

    /// <summary>Returns generated files without writing to disk — useful for preview.</summary>
    public GenerationResult Preview(GenerationRequest request)
        => Run(request, write: false);

    /// <summary>Generates all files and writes them to <see cref="GenerationRequest.OutputPath"/>.</summary>
    public GenerationResult Generate(GenerationRequest request)
        => Run(request, write: true);

    private GenerationResult Run(GenerationRequest request, bool write)
    {
        var result = new GenerationResult { Success = true };

        foreach (var generator in _generators)
        {
            try
            {
                result.Files.AddRange(generator.Generate(request));
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
                result.Success = false;
            }
        }

        if (write && result.Success)
        {
            fileWriter.Write(request.OutputPath, result.Files, result.Errors);
            result.WrittenToDisk = result.Errors.Count == 0;
            if (result.Errors.Count > 0) result.Success = false;
        }

        return result;
    }
}
