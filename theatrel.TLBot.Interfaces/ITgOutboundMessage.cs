﻿using Telegram.Bot.Types.ReplyMarkups;

namespace theatrel.TLBot.Interfaces
{
    public interface ITgOutboundMessage
    {
        string Message { get; set; }
        ReplyKeyboardMarkup ReplyKeyboard { get; set; }
        bool IsEscaped { get; set; }
    }
}
