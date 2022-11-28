using System.Linq;

namespace theatrel.Lib.MariinskyParsers;

internal static class StringExtentions
{
    public static string RemoveWhitespace(this string input)
    {
        return new string(input
            .ToCharArray()
            .Where(c => !char.IsWhiteSpace(c))
            .ToArray());
    }
}