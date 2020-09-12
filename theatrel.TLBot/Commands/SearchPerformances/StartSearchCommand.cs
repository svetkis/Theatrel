using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands.SearchPerformances
{
    internal class StartSearchCommand : DialogCommandBase
    {
        private static readonly string[] StartCommandVariants
            = { @"/start", "привет", "hi", "hello", "добрый день", "начать", "давай", "поищи", "да" };

        protected override string ReturnCommandMessage { get; set; } = string.Empty;

        public override string Name => "Старт";

        public StartSearchCommand(IDbService dbService) : base((int)DialogStep.Start, dbService)
        {
        }

        public override bool IsMessageCorrect(string message) => StartCommandVariants.Any(variant => message.ToLower().StartsWith(variant));

        public override Task<ITgOutboundMessage> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
            => Task.FromResult<ITgOutboundMessage>(new TgOutboundMessage(null));

        public override Task<ITgOutboundMessage> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
            => Task.FromResult<ITgOutboundMessage>(new TgOutboundMessage("Вас приветствует экономный театрал."));
    }
}
