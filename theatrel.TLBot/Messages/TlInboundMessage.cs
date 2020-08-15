using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Messages
{
    internal class TlInboundMessage : ITlInboundMessage
    {
        public long ChatId { get; set; }
        public string Message { get; set; }
    }
}
