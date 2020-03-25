using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Commands
{
    internal class StartCommand : DialogCommandBase
    {
        private static string[] _startCommandVariants
            = new[] { @"/start", "привет", "hi", "hello", "добрый день", "начать", "давай", "поищи" };

        public StartCommand() : base((int)DialogStep.Start)
        {
        }

        public override bool IsMessageClear(string message) => _startCommandVariants.Any(variant => message.ToLower().StartsWith(variant));

        public override string ApplyResult(IChatDataInfo chatInfo, string message)
        {
            Trace.TraceInformation($"reset chat {chatInfo}");
            chatInfo.Clear();

            return null;
        }

        public override async Task<string> ExecuteAsync(IChatDataInfo chatInfo) => "Вас привествует экономный театрал.";
    }
}
