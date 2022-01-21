using System.Collections.Generic;

namespace theatrel.Common.FormatHelper;

public class DaysOfWeekComparer : IComparer<int>
{
    public static IComparer<int> Create() => new DaysOfWeekComparer();

    public int Compare(int a, int b)
    {
        int c1 = a == 0 ? 7 : a;
        int c2 = b == 0 ? 7 : b;

        return c1 > c2 ? 1 : c1 < c2 ? -1 : 0;
    }
}