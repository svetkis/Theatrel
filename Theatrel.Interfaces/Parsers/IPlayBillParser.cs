using System.Threading.Tasks;

namespace theatrel.Interfaces.Parsers
{
    public interface IPlayBillParser : IDIRegistrableService
    {
        Task<IPerformanceData[]> Parse(string playbill);
    }
}
