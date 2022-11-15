using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Filters;

namespace theatrel.Lib.Filters;

public interface IFilterProcessor
{
    bool IsCorrectProcessor(IPerformanceFilter filter);
    bool IsChangeSuitable(PlaybillChangeEntity change, IPerformanceFilter filter);
    public PlaybillEntity[] GetFilteredPerformances(IPerformanceFilter filter);
}
