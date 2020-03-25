﻿using System.Threading.Tasks;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Commands
{
    public abstract class DialogCommandBase : IDialogCommand
    {
        public int Label { get; }

        protected DialogCommandBase(int label)
        {
            Label = label;
        }

        protected readonly char[] WordSplitters = new char[] { ' ', ',', '.' };

        public abstract bool IsMessageClear(string message);

        public abstract Task<string> ExecuteAsync(IChatDataInfo chatInfo);

        public abstract string ApplyResult(IChatDataInfo chatInfo, string message);
    }
}
