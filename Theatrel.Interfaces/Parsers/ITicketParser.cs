using AngleSharp.Dom;

namespace theatrel.Interfaces.Parsers
{
    public interface ITicketParser : IDIRegistrableService
    {
        ITicket Parse(IElement ticket);
    }
}
