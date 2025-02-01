using System.Data;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Softwrox.Forensics.Concordance.Core;

public static class DataTableExtensions
{
    public static string ToJson(this DataTable table)
    {
        var json = JsonConvert.SerializeObject(table, Formatting.Indented);
        return json;
    }

    public static void ExtractIntoNewColumn(this DataTable table, string fromColumn, string pattern, string toColumn, bool replaceOriginal = false)
    {
        table.Columns.Add(toColumn, typeof(string));
        foreach (DataRow row in table.Rows)
        {
            Match match = Regex.Match(row[fromColumn]?.ToString() ?? string.Empty, pattern);
            row[toColumn] = match.Success ? match.Groups[1].Value : string.Empty;
        }
        if (replaceOriginal)
        {
            table.Columns.Remove(fromColumn);
        }
    }

    public static void ExtractIntoExistingColumn(this DataTable table, string fromColumn, string pattern, string toColumn)
    {
        foreach (DataRow row in table.Rows)
        {
            Match match = Regex.Match(row[fromColumn]?.ToString() ?? string.Empty, pattern);
            row[toColumn] = match.Success ? match.Groups[1].Value : string.Empty;
        }
    }

}
