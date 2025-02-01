using System.Data;

namespace Softwrox.Forensics.Concordance.Core;

public static class ConcordanceFileReader
{
    public static DataTable Read(string path)
    {
        try
        {
            var columnNames = GetColumnNames(path);
            DataTable table = InitializeTable(columnNames);
            PopulateTableWithData(table, path);
            return table;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to read the file at {path}.", exception);
        }
    }

    private static string[] GetColumnNames(string path)
    {
        string firstLine = File.ReadLines(path).First();
        return firstLine.Split(ConcordanceFileSettings.Column)
            .Select(column => column.Trim().Trim(ConcordanceFileSettings.Quote))
            .ToArray();
    }

    private static DataTable InitializeTable(string[] columnNames)
    {
        DataTable table = new DataTable("ConcordanceFileData");
        foreach (string columnName in columnNames)
        {
            table.Columns.Add(columnName, typeof(string));
        }
        return table;
    }

    private static void PopulateTableWithData(DataTable table, string path)
    {
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var values = ParseLine(line);
            table.Rows.Add(values);
        }
    }

    private static string[] ParseLine(string line)
    {
        return line.Split(ConcordanceFileSettings.Column)
            .Select(value => value.Trim().Trim(ConcordanceFileSettings.Quote))
            .ToArray();
    }
}
