using Telegram.Bot.Types.ReplyMarkups;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Messages
{
    internal class TgCommandResponse : TgOutboundMessage, ITgCommandResponse
    {
        public TgCommandResponse(string message, ReplyKeyboardMarkup replyKeyboard = null) : base(message, replyKeyboard)
        {
            Message = message;
            ReplyKeyboard = replyKeyboard;
        }

        public bool NeedToRepeat { get; set; }
    }
}
