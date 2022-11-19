﻿using System.Globalization;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Autofac;

namespace theatrel.Lib.Interfaces;

public interface IDescriptionService : IDIRegistrable
{
    string GetCastDescription(PlaybillEntity playbillEntity, string castAdded, string castRemoved);
    string GetPerformanceDescription(PlaybillEntity playbillEntity, int lastMinPrice, CultureInfo culture);
}