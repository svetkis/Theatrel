using Telegram.Bot.Types.ReplyMarkups;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Commands
{
    internal class TlCommandResponse : ICommandResponse
    {
        public TlCommandResponse(string message, ReplyKeyboardMarkup replyKeyboard = null)
        {
            Message = message;
            ReplyKeyboard = replyKeyboard;
        }

        public string Message { get; set; }
        public ReplyKeyboardMarkup ReplyKeyboard { get; set; }
    }
}
