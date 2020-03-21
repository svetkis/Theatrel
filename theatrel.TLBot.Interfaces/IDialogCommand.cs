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
        void ApplyResult(IChatDataInfo chatInfo, string message);
        bool CanExecute(string message);
        Task<string> ExecuteAsync(IChatDataInfo chatInfo);
    }
}
