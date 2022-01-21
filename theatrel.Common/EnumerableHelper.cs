using System;
using System.Collections.Generic;

namespace theatrel.Common;

public static class EnumerableHelper
{
    public static int IndexWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        int index = 0;
        foreach (T element in source)
        {
            if (predicate(element))
            {
                return index;
            }
            ++index;
        }

        return -1;
    }
}