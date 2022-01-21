namespace theatrel.TLBot.Interfaces;

public interface ITgCommandResponse : ITgOutboundMessage
{
    bool NeedToRepeat { get; set; }
}