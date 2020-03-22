using System.Threading.Tasks;

namespace theatrel.Interfaces.Parsers
{
    public interface IPlayBillParser : IDIRegistrable
    {
        Task<IPerformanceData[]> Parse(string playbill);
    }
}
