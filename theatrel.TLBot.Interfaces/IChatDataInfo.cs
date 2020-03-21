using System;

namespace theatrel.TLBot.Interfaces
{
    public interface IChatDataInfo
    {
        int ChatStep { get; set; }
        DateTime When { get; set; }
        DayOfWeek[] Days { get; set; }
        string[] Types { get; set; }

        DateTime LastMessage {get; set;}

        void Clear();
    }
}
