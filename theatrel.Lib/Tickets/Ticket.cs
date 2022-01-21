using theatrel.Interfaces.Tickets;

namespace theatrel.Lib.Tickets;

internal class Ticket : ITicket
{
    public string Id { get; set; }
    public string Region { get; set; }
    public string Side { get; set; }
    public string Row { get; set; }
    public string Place { get; set; }
    public int MinPrice { get; set; }
}