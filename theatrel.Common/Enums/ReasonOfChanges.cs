﻿using System;
using System.ComponentModel;

namespace theatrel.Common.Enums
{
    [Flags]
    public enum ReasonOfChanges
    {
        [Description("Ничего не поменялось")]
        NothingChanged = 0,

        [Description("Появление в афише")]
        Creation = 1,

        [Description("Снижение цены")]
        PriceDecreased = 2,

        [Description("Повышение цены")]
        PriceIncreased = 4,

        [Description("Начало продаж")]
        StartSales = 8,

        [Description("Стоп продаж")]
        PriceBecameZero = 16,
    }
}
