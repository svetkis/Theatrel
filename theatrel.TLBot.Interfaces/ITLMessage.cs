namespace theatrel.TLBot.Interfaces
{
    public interface ITLMessage
    {
        long ChatId { get; set; }
        string Message { get; set; }
    }
}
