using Telegram.Bot.Types.ReplyMarkups;

namespace theatrel.TLBot.Interfaces
{
    public interface ICommandResponse
    {
        string Message { get; set; }
        ReplyKeyboardMarkup ReplyKeyboard { get; set; }
    }
}
