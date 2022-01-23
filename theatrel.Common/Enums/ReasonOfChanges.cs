using System;
using System.ComponentModel;

namespace theatrel.Common.Enums;

[Flags]
public enum ReasonOfChanges
{
    [Description("Ничего не поменялось")]
    None = 0,

    [Description("Появление в афише")]
    Creation = 1,

    [Description("Снижение цены")]
    PriceDecreased = 2,

    [Description("Повышение цены")]
    PriceIncreased = 4,

    [Description("Начало продаж")]
    StartSales = 8,

    [Description("Стоп продаж")]
    StopSale = 16,

    [Description("Ошибка получения данных")]
    DataError = 32,

    [Description("Билеты закончились")]
    StopSales = 64,

    [Description("Переносится")]
    WasMoved = 128,

    [Description("Обьявлен состав испольнителей")]
    CastWasSet = 256,

    [Description("Состав исполнителей был изменен")]
    CastWasChanged = 512,
}