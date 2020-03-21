using System.Linq;
using System.Text;
using System.Threading.Tasks;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Commands
{
    internal class PerfomanceTypesCommand : DialogCommandBase
    {
        private string[] _types = new[] { "опера", "балет", "концерт" };
        private string[] _every = new[] { "все", "всё", "любой", "любое", "не важно"};

        public PerfomanceTypesCommand() : base((int)DialogStep.SelectType)
        { }

        public override void ApplyResult(IChatDataInfo chatInfo, string message)
        {
            chatInfo.Types = ParseMessage(message);
        }

        public override bool CanExecute(string message) => SplitMessage(message).Any();

        public override async Task<string> ExecuteAsync(IChatDataInfo chatInfo)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Вас интересует {string.Join(" или ", _types)}?");
            return stringBuilder.ToString();
        }

        private string[] ParseMessage(string message)
        {
            var parts = SplitMessage(message);
            if (parts.Any(p => _every.Any(e => e.ToLower().Contains(p.ToLower()))))
                return null;

            return parts.Select(ParseMessagePart).Where(idx => idx > -1).Select(idx => _types[idx]).ToArray();
        }

        private string[] SplitMessage(string message) => message.Split(WordSplitters).Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

        private int ParseMessagePart(string messagePart)
        {
            if (string.IsNullOrWhiteSpace(messagePart))
                return -1;

            return CheckEnumarable(_types, messagePart);
        }

        private int CheckEnumarable(string[] checkedData, string msg)
        {
            var data = checkedData.Select((item, idx) => new { idx, item })
                .FirstOrDefault(data => 0 == string.Compare(data.item, msg, true));

            return null != data ? data.idx : -1;
        }
    }
}
