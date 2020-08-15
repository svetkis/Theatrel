namespace theatrel.TLBot.Interfaces
{
    public interface ITlInboundMessage
    {
        long ChatId { get; set; }
        string Message { get; set; }
    }
}
