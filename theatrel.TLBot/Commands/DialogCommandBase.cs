using System.Threading;
using System.Threading.Tasks;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Commands
{
    public abstract class DialogCommandBase : IDialogCommand
    {
        protected const string ReturnMsg = "Для того что бы выбрать другой вариант напишите Нет.";

        public int Label { get; }

        protected DialogCommandBase(int label)
        {
            Label = label;
        }

        protected readonly char[] WordSplitters = { ' ', ',', '.' };

        public abstract bool IsMessageReturnToStart(string message);

        public abstract Task<string> ExecuteAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken);

        public abstract Task<string> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken);
    }
}
