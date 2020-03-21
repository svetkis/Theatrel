using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot
{
    internal class TLMessage : ITLMessage
    {
        public long ChatId { get; set; }
        public string Message { get; set; }
    }
}
