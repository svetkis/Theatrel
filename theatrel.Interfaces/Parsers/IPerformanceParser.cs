namespace theatrel.Interfaces.Parsers
{
    public interface IPerformanceParser : IDIRegistrable
    {
        IPerformanceData Parse(AngleSharp.Dom.IElement element);
    }
}
