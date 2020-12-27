using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.TgBot;

namespace theatrel.TLBot.Interfaces
{
    public interface IDialogCommand
    {
        string Name { get; }

        Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken);
        Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken);

        bool IsMessageCorrect(IChatDataInfo chatInfo, string message);
        bool IsReturnCommand(string message);
    }
}
