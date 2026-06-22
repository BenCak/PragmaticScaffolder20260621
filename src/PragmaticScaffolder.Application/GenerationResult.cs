namespace PragmaticScaffolder.Application;

public sealed class GenerationResult
{
    public bool Success { get; set; } = true;
    public bool WrittenToDisk { get; set; }
    public List<GeneratedFile> Files { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}
