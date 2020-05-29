using theatrel.Interfaces;

namespace theatrel.TLBot.Interfaces
{
    public interface IFilterHelper : IDISingleton
    {
        IPerformanceFilter GetFilter(IChatDataInfo dataInfo);
    }
}
