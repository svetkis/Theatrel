using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Messages
{
    internal class TgInboundMessage : ITgInboundMessage
    {
        public long ChatId { get; set; }
        public string Message { get; set; }
    }
}
