using System.Threading;
using System.Threading.Tasks;

namespace theatrel.TLBot.Interfaces
{
    public interface IDialogCommand
    {
        int Label { get; }
        Task<ICommandResponse> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken);
        Task<ICommandResponse> AscUserAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken);

        bool IsMessageReturnToStart(string message);
        bool IsReturnCommand(string message);
    }
}
