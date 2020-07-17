namespace theatrel.Interfaces.Parsers
{
    public interface IPerformanceParser : IDIRegistrable
    {
        IPerformanceData Parse(object element);
    }
}
