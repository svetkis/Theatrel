using Telegram.Bot.Types.ReplyMarkups;

namespace theatrel.TLBot.Interfaces
{
    public interface ITgCommandResponse
    {
        string Message { get; set; }
        ReplyKeyboardMarkup ReplyKeyboard { get; set; }
        bool IsEscaped { get; set; }
        bool NeedToRepeat { get; set; }
    }
}
