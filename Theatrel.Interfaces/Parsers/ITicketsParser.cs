using System.Threading.Tasks;

namespace theatrel.Interfaces.Parsers
{
    public interface ITicketsParser : IDIRegistrableService
    {
        Task<IPerfomanceTickets> Parse(string data);
        Task<IPerfomanceTickets> ParseFromUrl(string url);
    }
}
