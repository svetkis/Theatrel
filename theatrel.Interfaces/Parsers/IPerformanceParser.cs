using theatrel.Interfaces.Playbill;

namespace theatrel.Interfaces.Parsers
{
    public interface IPerformanceParser
    {
        IPerformanceData Parse(object element, int year, int month);
    }
}
