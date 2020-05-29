using System;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot
{
    public class ChatDataInfo : IChatDataInfo
    {
        public long ChatId { get; set; }

        public string Culture { get; set; }
        public int ChatStep { get; set; }

        public DateTime When { get; set; }
        public DayOfWeek[] Days { get; set; }
        public string[] Types { get; set; }

        public DateTime LastMessage { get; set; }
        public DialogStateEnum DialogState { get; set; }

        public void Clear()
        {
            ChatStep = 0;
            Days = null;
            Types = null;
            DialogState = DialogStateEnum.DialogStarted;

            LastMessage = new DateTime();
        }
    }
}
