namespace Softwrox.Forensics.Concordance.Core;

static class StringExtensions
{
    public static string Wrap(this string text, char affix)
    {
        return $"{affix}{text}{affix}";
    }
}
