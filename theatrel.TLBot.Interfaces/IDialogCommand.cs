using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces;

namespace theatrel.TLBot.Interfaces
{
    public interface IDialogCommand
    {
        int Label { get; }
        string Name { get; }

        Task<ITlOutboundMessage> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken);
        Task<ITlOutboundMessage> AscUserAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken);

        bool IsMessageCorrect(string message);
        bool IsReturnCommand(string message);
    }
}
