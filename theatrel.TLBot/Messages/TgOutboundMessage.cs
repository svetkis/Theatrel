using Telegram.Bot.Types.ReplyMarkups;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Messages;

internal class TgOutboundMessage : ITgOutboundMessage
{
    public TgOutboundMessage(string message, ReplyKeyboardMarkup replyKeyboard = null)
    {
        Message = message;
        ReplyKeyboard = replyKeyboard;
    }

    public string Message { get; set; }
    public ReplyKeyboardMarkup ReplyKeyboard { get; set; }
    public bool IsEscaped { get; set; }
}