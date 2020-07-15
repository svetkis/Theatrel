using System.Threading;
using System.Threading.Tasks;

namespace theatrel.TLBot.Interfaces
{
    public interface IDialogCommand
    {
        int Label { get; }
        Task<string> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken);
        bool IsMessageReturnToStart(string message);
        Task<string> ExecuteAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken);
    }
}
