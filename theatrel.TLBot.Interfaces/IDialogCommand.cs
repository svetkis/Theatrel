﻿using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.TgBot;

namespace theatrel.TLBot.Interfaces
{
    public interface IDialogCommand
    {
        int Label { get; }
        string Name { get; }

        Task<ITgOutboundMessage> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken);
        Task<ITgOutboundMessage> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken);

        bool IsMessageCorrect(string message);
        bool IsReturnCommand(string message);
    }
}
