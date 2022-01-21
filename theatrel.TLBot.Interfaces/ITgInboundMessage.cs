namespace theatrel.TLBot.Interfaces;

public interface ITgInboundMessage
{
    long ChatId { get; set; }
    string Message { get; set; }
}