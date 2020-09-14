using Telegram.Bot.Types.ReplyMarkups;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Messages
{
    internal class TgCommandResponse : ITgCommandResponse
    {
        public TgCommandResponse(string message, ReplyKeyboardMarkup replyKeyboard = null)
        {
            Message = message;
            ReplyKeyboard = replyKeyboard;
        }

        public string Message { get; set; }
        public ReplyKeyboardMarkup ReplyKeyboard { get; set; }
        public bool IsEscaped { get; set; }
        public bool NeedToRepeat { get; set; }
    }
}
