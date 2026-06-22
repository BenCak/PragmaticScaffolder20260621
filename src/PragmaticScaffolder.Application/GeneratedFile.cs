namespace PragmaticScaffolder.Application;

public sealed class GeneratedFile
{
    /// <summary>Path relative to the output root (e.g. "src/MyApp.Web/Features/Customers/CustomerService.cs").</summary>
    public string RelativePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
