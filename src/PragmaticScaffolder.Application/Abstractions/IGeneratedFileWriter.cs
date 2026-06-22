namespace PragmaticScaffolder.Application.Abstractions;

/// <summary>Writes generated files to their final destination (e.g. disk).</summary>
public interface IGeneratedFileWriter
{
    /// <summary>Writes <paramref name="files"/> under <paramref name="outputPath"/>, appending any failures to <paramref name="errors"/>.</summary>
    void Write(string outputPath, IReadOnlyList<GeneratedFile> files, List<string> errors);
}
