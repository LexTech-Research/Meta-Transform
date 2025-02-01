using System.Data;
using System.Text;

namespace Softwrox.Forensics.Concordance.Core;

public static class ConcordanceFileWriter
{
    public static void Write(DataTable data, string path)
    {
        var content = new StringBuilder();
        AppendHeaders(content, data);
        AppendRows(content, data);
        SaveToFile(content, path);
    }

    private static void AppendHeaders(StringBuilder content, DataTable data)
    {
        string headerLine = string.Join(
            ConcordanceFileSettings.Column,
            data.Columns.ColumnNames().Wrap(ConcordanceFileSettings.Quote)
        );
        content.AppendLine(headerLine);
    }

    private static void AppendRows(StringBuilder content, DataTable data)
    {
        foreach (DataRow row in data.Rows)
        {
            string rowLine = string.Join(
                ConcordanceFileSettings.Column,
                data.Columns.ColumnNames()
                    .Select(name => row[name]?.ToString() ?? string.Empty)
                    .Wrap(ConcordanceFileSettings.Quote)
            );
            content.AppendLine(rowLine);
        }
    }

    private static void SaveToFile(StringBuilder content, string path)
    {
        try
        {
            File.WriteAllText(path, content.ToString());
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to write to file at {path}.", exception);
        }
    }
}
