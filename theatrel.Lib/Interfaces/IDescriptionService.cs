using System.Globalization;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.TgBot;

namespace theatrel.Lib.Interfaces;

public interface IDescriptionService : IDIRegistrable
{
    string CreatePerformancesMessage(IChatDataInfo chatInfo, PlaybillEntity[] performances, bool includeCast);
    string GetCastDescription(PlaybillEntity playbillEntity, string castAdded, string castRemoved);
    string GetPerformanceDescription(PlaybillEntity playbillEntity, int lastMinPrice, CultureInfo culture);
}
