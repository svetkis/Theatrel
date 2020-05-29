using System;

namespace theatrel.TLBot.Interfaces
{
    public enum DialogStateEnum
    {
        DialogStarted,
        DialogReturned
    }

    public interface IChatDataInfo
    {
        long ChatId { get; set; }
        string Culture { get; set; }
        int ChatStep { get; set; }
        DateTime When { get; set; }
        DayOfWeek[] Days { get; set; }
        string[] Types { get; set; }
        DateTime LastMessage {get; set;}
        DialogStateEnum DialogState { get; set; }

        void Clear();
    }
}
