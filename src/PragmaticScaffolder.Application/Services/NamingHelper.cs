using System.Text;
using System.Text.RegularExpressions;
using PragmaticScaffolder.Domain.Models;

namespace PragmaticScaffolder.Application.Services;

/// <summary>Single source of truth for all identifier transforms used in code generation.</summary>
public static partial class NamingHelper
{
    private static readonly HashSet<string> CSharpKeywords =
    [
        "abstract","as","base","bool","break","byte","case","catch","char","checked",
        "class","const","continue","decimal","default","delegate","do","double","else",
        "enum","event","explicit","extern","false","finally","fixed","float","for",
        "foreach","goto","if","implicit","in","int","interface","internal","is","lock",
        "long","namespace","new","null","object","operator","out","override","params",
        "private","protected","public","readonly","ref","return","sbyte","sealed",
        "short","sizeof","stackalloc","static","string","struct","switch","this",
        "throw","true","try","typeof","uint","ulong","unchecked","unsafe","ushort",
        "using","virtual","void","volatile","while"
    ];

    // Simple pluralisation — good enough for generated code
    private static readonly Dictionary<string, string> IrregularPlurals = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Person"] = "People", ["Child"] = "Children", ["Man"] = "Men",
        ["Woman"] = "Women", ["Tooth"] = "Teeth", ["Foot"] = "Feet",
        ["Mouse"] = "Mice", ["Goose"] = "Geese"
    };

    /// <summary>Convert a table name to a PascalCase singular class name.</summary>
    public static string ToClassName(string tableName, string prefix = "")
    {
        var pascal = ToPascalCase(StripPrefix(tableName, prefix));
        var singular = Singularize(pascal);
        return char.IsDigit(singular[0]) ? $"Entity{singular}" : singular;
    }

    private static string StripPrefix(string name, string prefix)
    {
        if (string.IsNullOrEmpty(prefix)) return name;
        return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? name[prefix.Length..]
            : name;
    }

    /// <summary>Convert a column name to a PascalCase property name.</summary>
    public static string ToPropertyName(string columnName)
    {
        var pascal = ToPascalCase(columnName);
        if (char.IsDigit(pascal[0])) return $"Col{pascal}";
        if (CSharpKeywords.Contains(pascal.ToLowerInvariant())) return $"{pascal}Value";
        return pascal;
    }

    /// <summary>Plural PascalCase class name for a collection property (singularizes first).</summary>
    public static string ToCollectionName(string tableName, string prefix = "")
    {
        var singular = ToClassName(tableName, prefix);
        return Pluralize(singular);
    }

    /// <summary>camelCase parameter name safe to use in method signatures.</summary>
    public static string ToParamName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return propertyName;
        var camel = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        return CSharpKeywords.Contains(camel) ? $"@{camel}" : camel;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static string ToPascalCase(string name)
    {
        var sb = new StringBuilder();
        var nextUpper = true;
        foreach (var ch in name)
        {
            if (ch is '_' or ' ' or '-')
            {
                nextUpper = true;
                continue;
            }
            sb.Append(nextUpper ? char.ToUpperInvariant(ch) : ch);
            nextUpper = false;
        }
        return sb.Length == 0 ? name : sb.ToString();
    }

    private static string Singularize(string word)
    {
        if (IrregularPlurals.ContainsValue(word))
        {
            var key = IrregularPlurals.First(kvp => kvp.Value.Equals(word, StringComparison.OrdinalIgnoreCase)).Key;
            return key;
        }
        if (word.EndsWith("ies", StringComparison.OrdinalIgnoreCase) && word.Length > 4)
            return word[..^3] + "y";
        if (word.EndsWith("ses", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("xes", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("zes", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("ches", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("shes", StringComparison.OrdinalIgnoreCase))
            return word[..^2];
        if (word.EndsWith('s') && !word.EndsWith("ss") && word.Length > 2)
            return word[..^1];
        return word;
    }

    private static string Pluralize(string word)
    {
        if (IrregularPlurals.TryGetValue(word, out var irregular)) return irregular;
        if (word.EndsWith('y') && word.Length > 1 && !IsVowel(word[^2]))
            return word[..^1] + "ies";
        if (word.EndsWith('s') || word.EndsWith('x') || word.EndsWith('z') ||
            word.EndsWith("ch") || word.EndsWith("sh"))
            return word + "es";
        return word + "s";
    }

    private static bool IsVowel(char c) => "aeiouAEIOU".Contains(c);

    /// <summary>
    /// Finds the best human-readable display column on a referenced table for FK joins.
    /// Prefers columns named "Name", "[Entity]Name", "Title" — falls back to first non-PK string column.
    /// Returns null if no suitable string column exists.
    /// </summary>
    public static ColumnMetadata? FindDisplayColumn(TableMetadata refTable, string prefix = "")
    {
        var refEntity = ToClassName(refTable.Name, prefix);
        var stringCols = refTable.Columns
            .Where(c => !c.IsPrimaryKey && c.ClrType is "string" or "string?")
            .ToList();
        if (stringCols.Count == 0) return null;

        // Priority list of preferred column name patterns
        string[] preferred = ["Name", "Title", "Description", $"{refEntity}Name", $"{refEntity}Title"];
        foreach (var candidate in preferred)
        {
            var col = stringCols.FirstOrDefault(c =>
                c.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase));
            if (col is not null) return col;
        }

        // Any column whose name contains "Name"
        var nameCol = stringCols.FirstOrDefault(c =>
            c.Name.Contains("Name", StringComparison.OrdinalIgnoreCase));
        if (nameCol is not null) return nameCol;

        return stringCols[0];
    }

    /// <summary>
    /// Parses a stored procedure name into feature folder, entity class name, and action name.
    /// "usp_tblOrders_Search" with spPrefix="usp_" tablePrefix="tbl"
    ///   → featureFolder="Orders", className="Order", actionName="Search"
    /// </summary>
    public static (string FeatureFolder, string ClassName, string ActionName) ParseProcName(
        string procName, string spPrefix, string tablePrefix)
    {
        var withoutSp  = StripPrefix(procName, spPrefix);
        var withoutTbl = StripPrefix(withoutSp, tablePrefix);
        var idx        = withoutTbl.IndexOf('_');
        var tablePart  = idx >= 0 ? withoutTbl[..idx]      : withoutTbl;
        var actionPart = idx >= 0 ? withoutTbl[(idx + 1)..] : "Execute";
        return (
            FeatureFolder: ToCollectionName(tablePart),
            ClassName:     ToClassName(tablePart),
            ActionName:    ToPascalCase(actionPart)
        );
    }

    /// <summary>
    /// Converts a PascalCase column name to a human-readable label.
    /// "CompanyName" → "Company Name", "CustomerID" → "Customer ID".
    /// </summary>
    public static string ToLabel(string columnName) =>
        Regex.Replace(columnName, @"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");

    /// <summary>
    /// Derives the DtoEx display property name from a FK column name.
    /// "CustomerID" → base "Customer", + display col "CompanyName" → "CustomerCompanyName".
    /// </summary>
    public static string ToDtoExPropertyName(string fkColumnName, string displayColumnName)
    {
        var prop = ToPropertyName(fkColumnName);
        // Strip trailing Id/ID/id suffix to get the base name
        var baseName = prop.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
            ? prop[..^2]
            : prop;
        return baseName + ToPropertyName(displayColumnName);
    }
}
