namespace theatrel.Interfaces.Parsers
{
    public interface IPerformanceParser : IDIRegistrableService
    {
        IPerformanceData Parse(AngleSharp.Dom.IElement element);
    }
}
