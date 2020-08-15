using Telegram.Bot.Types.ReplyMarkups;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Messages
{
    internal class TlOutboundMessage : ITlOutboundMessage
    {
        public TlOutboundMessage(string message, ReplyKeyboardMarkup replyKeyboard = null)
        {
            Message = message;
            ReplyKeyboard = replyKeyboard;
        }

        public string Message { get; set; }
        public ReplyKeyboardMarkup ReplyKeyboard { get; set; }
        public bool IsEscaped { get; set; }
    }
}
