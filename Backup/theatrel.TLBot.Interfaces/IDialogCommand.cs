using System.Threading.Tasks;

namespace theatrel.TLBot.Interfaces
{
    public enum DialogAction
    {
        ReturnToStart,
        Previous,
        Next,
        Custom,
        GoToFinish
    }

    public interface IDialogCommand
    {
        int Label { get; }
        string ApplyResult(IChatDataInfo chatInfo, string message);
        bool IsMessageClear(string message);
        Task<string> ExecuteAsync(IChatDataInfo chatInfo);
    }
}
