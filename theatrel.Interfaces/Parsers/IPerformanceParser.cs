using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.Playbill;

namespace theatrel.Interfaces.Parsers
{
    public interface IPerformanceParser : IDIRegistrable
    {
        IPerformanceData Parse(object element);
    }
}
