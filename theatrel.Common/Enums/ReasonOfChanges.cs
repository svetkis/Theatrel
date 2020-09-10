using System;

namespace theatrel.Common.Enums
{
    [Flags]
    public enum ReasonOfChanges
    {
        NothingChanged = 0,
        Creation = 1,
        PriceDecreased = 2,
        PriceIncreased = 4,
        StartSales = 8,
        PriceBecameZero = 16,
    }
}
