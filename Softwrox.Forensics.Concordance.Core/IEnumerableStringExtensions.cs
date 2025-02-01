using System.Data;

namespace Softwrox.Forensics.Concordance.Core;

static class IEnumerableStringExtensions
{
    public static IEnumerable<string> Wrap(this IEnumerable<string> items, char affix)
    {
        return items.Select(item => item.Wrap(affix));
    }
}
