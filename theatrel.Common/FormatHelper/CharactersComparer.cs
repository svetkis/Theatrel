using System;
using System.Collections.Generic;

namespace theatrel.Common.FormatHelper;

public class CharactersComparer : IComparer<string>
{
    public static IComparer<string> Create() => new CharactersComparer();

    public int Compare(string a, string b)
    {
        bool conductorA = string.Equals(a, CommonTags.Conductor);
        bool conductorB = string.Equals(b, CommonTags.Conductor);
        if (conductorA && conductorB)
            return 0;

        if (conductorA)
            return -1;

        if (conductorB)
            return 1;

        return string.Compare(a, b, StringComparison.Ordinal);
    }
}