using System.Threading.Tasks;

namespace theatrel.Interfaces.Parsers
{
    public interface ITicketsParser : IDIRegistrable
    {
        Task<IPerfomanceTickets> Parse(string data);
        Task<IPerfomanceTickets> ParseFromUrl(string url);
    }
}
