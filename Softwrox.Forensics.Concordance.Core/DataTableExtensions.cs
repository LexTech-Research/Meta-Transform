using System.Data;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Softwrox.Forensics.Concordance.Core;

public static class DataTableExtensions
{
    public static void ExtractIntoExistingColumn(this DataTable table, string fromColumn, string pattern, string toColumn)
    {
        foreach (DataRow row in table.Rows)
        {
            Match match = Regex.Match(row[fromColumn]?.ToString() ?? string.Empty, pattern);
            row[toColumn] = match.Success ? match.Groups[1].Value : string.Empty;
        }
    }

    public static DataTable DeleteColumn(this DataTable table, string column)
    {        
        if (!table.Columns.Contains(column))
        {
            return table.Copy();
        }

        var copy = table.Copy();
        copy.Columns.Remove(column);
        return copy;
    }

    public static DataTable AddColumn(this DataTable table, string column)
    {
        var copy = table.Copy();
        copy.Columns.Add(column);
        return copy;
    }

    public static DataTable DeleteRows(this DataTable table, string query)
    {
        var copy = table.Copy();
        foreach (var row in copy.Select(query))
            copy.Rows.Remove(row);
        return copy;
    }
}
