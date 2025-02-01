using System.Data;

namespace Softwrox.Forensics.Concordance.Core;

static class DataColumnCollectionExtensions
{
    public static IEnumerable<string> ColumnNames(this DataColumnCollection columns)
    {
        for (int i = 0; i < columns.Count; i++)
        {
            yield return columns[i].ColumnName;
        }
    }
}
