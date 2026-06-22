using Telerik.SvgIcons;

namespace PragmaticScaffolder.Web;

public sealed record ConnectionProfile(
    string Name,
    ISvgIcon Icon,
    string ConnectionString,
    string DefaultNamespace,
    string DefaultOutputPath,
    string TablePrefix = "",
    string SpPrefix    = "usp_"
);

/// <summary>Built-in quick-connect profiles for local development.</summary>
public static class ConnectionProfiles
{
    public const string NorthwindConnStr =
        "Data Source=localhost;Initial Catalog=northwind;User ID=sa;Password=Sa123465!;" +
        "Pooling=False;Connect Timeout=30;Encrypt=False;" +
        "Trust Server Certificate=True;Authentication=SqlPassword;" +
        "Application Name=vscode-mssql;Application Intent=ReadWrite;Command Timeout=30";

    public static readonly ConnectionProfile[] All =
    [
        new(
            Name              : "Northwind (local)",
            Icon              : SvgIcon.Link,
            ConnectionString  : NorthwindConnStr,
            DefaultNamespace  : "nws",
            DefaultOutputPath : "/home/ben/code/nws"
        )
    ];
}
