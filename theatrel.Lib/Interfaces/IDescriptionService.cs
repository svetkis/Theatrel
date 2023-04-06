using System.Collections.Generic;
using System.Globalization;
using theatrel.Common.Enums;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Autofac;

namespace theatrel.Lib.Interfaces;

public interface IDescriptionService : IDIRegistrable
{
    string GetPerformancesMessage(
        IEnumerable<PlaybillEntity> performances,
        CultureInfo culture,
        bool includeCast,
        out string performanceIdsList);

    string GetTgCastDescription(PlaybillEntity playbillEntity, string castAdded, string castRemoved);

    string GetTgPerformanceDescription(PlaybillEntity playbillEntity,
        int lastMinPrice,
        CultureInfo culture,
        ReasonOfChanges[] reasonOfChanges);

    string GetVkCastDescription(PlaybillEntity playbillEntity, string castAdded, string castRemoved);

    string GetVkPerformanceDescription(PlaybillEntity playbillEntity,
        int lastMinPrice,
        CultureInfo culture,
        ReasonOfChanges[] reasonOfChanges);
}
