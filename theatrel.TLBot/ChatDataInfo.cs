using System;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot
{
    public class ChatDataInfo : IChatDataInfo
    {
        public int ChatStep { get; set; }

        public DateTime When { get; set; }
        public DayOfWeek[] Days { get; set; }
        public string[] Types { get; set; }

        public DateTime LastMessage { get; set; }

        public void Clear()
        {
            ChatStep = 0;
            Days = null;
            Types = null;

            LastMessage = new DateTime();
        }
    }
}
