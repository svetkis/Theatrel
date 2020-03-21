using theatrel.Interfaces;

namespace theatrel.TLBot.Interfaces
{
    public interface IFilterHelper : IDIRegistrableService
    {
        IPerformanceFilter GetFilter(IChatDataInfo dataInfo);
    }
}
