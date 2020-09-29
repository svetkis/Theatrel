namespace theatrel.Interfaces.Tickets
{
    public interface ITicket
    {
        string Id { get; set; }
        string Region { get; set; }
        string Side { get; set; }
        string Row { get; set; }
        string Place { get; set; }

        int MinPrice { get; set; }
    }
}
